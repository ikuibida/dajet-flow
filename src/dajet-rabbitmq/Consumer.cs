using DaJet.Flow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DaJet.RabbitMQ
{
    public sealed class Consumer : Source<Message>, IConfigurable
    {
        private BrokerOptions? _options = new();

        private CancellationToken _token;

        private string? _consumerTag;
        private IModel? _channel;
        private IConnection? _connection;
        private EventingBasicConsumer? _consumer;

        private ILogger? _logger;
        private readonly IServiceProvider _serviceProvider;
        private int _consumed = 0;
        private Stopwatch watch = new Stopwatch();

        [ActivatorUtilitiesConstructor] public Consumer(IPipeline pipeline)
        {
            _serviceProvider = pipeline.Services;
        }
        public void Configure(Dictionary<string, string> options)
        {
            _logger = _serviceProvider.GetService<ILogger<Consumer>>();

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = BrokerOptions.CreateOptions(options);
        }
        public override void Pump(CancellationToken token)
        {
            _token = token;

            while (!_token.IsCancellationRequested)
            {
                try
                {
                    InitializeConsumer();

                    Task.Delay(TimeSpan.FromSeconds(10), _token).Wait(_token);

                    //_logger("Consumer heartbeat.");
                }
                catch (Exception error)
                {

                    throw;
                    //_logger(ExceptionHelper.GetErrorText(error));
                }
            }
        }
        protected override void _Dispose()
        {
            if (_consumer != null)
            {
                _consumer.Received -= ProcessMessage;
                _consumer.Model = null;
                _consumer = null;
            }

            _channel?.Dispose();
            _channel = null;

            _connection?.Dispose();
            _connection = null;
        }

        private void InitializeConnection()
        {
            if (_connection != null && _connection.IsOpen)
            {
                return;
            }

            _connection?.Dispose(); // The connection might be closed, but not disposed yet.

            IConnectionFactory factory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                Port = _options.HostPort,
                VirtualHost = _options.VirtualHost,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = factory.CreateConnection();
        }
        private void InitializeChannel()
        {
            if (_channel != null && _channel.IsOpen)
            {
                return;
            }

            _channel?.Dispose(); // The channel might be closed, but not disposed yet.

            InitializeConnection();

            _channel = _connection!.CreateModel();
            _channel.BasicQos(0, 1, false);
        }
        private void InitializeConsumer()
        {
            if (_consumer != null &&
                _consumer.Model != null &&
                _consumer.Model.IsOpen &&
                _consumer.IsRunning)
            {
                return;
            }

            if (_consumer != null)
            {
                _consumer.Received -= ProcessMessage;
                _consumer.Model?.Dispose();
                _consumer.Model = null;
            }

            InitializeChannel();

            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += ProcessMessage;

            _consumerTag = _channel.BasicConsume(_options.RoutingKey, false, _consumer);

            //_consumer.Model.BasicCancel(_consumerTag);
        }
        private void ProcessMessage(object sender, BasicDeliverEventArgs args)
        {
            if (sender is not EventingBasicConsumer consumer) return;

            bool success = true;

            try
            {
                Message message = CreateMessage(in args);

                _Process(in message);

                _Synchronize();

                consumer.Model.BasicAck(args.DeliveryTag, false);

                
                if (_consumed == 0)
                {
                    watch.Start();
                }

                _consumed++;

                if (_consumed == 1000)
                {
                    watch.Stop();
                    _logger?.LogInformation($"[RabbitMQ.Consumer] Consumed {_consumed} messages in {watch.ElapsedMilliseconds} milliseconds.");
                    _consumed = 0;
                    watch.Reset();
                }
            }
            catch (Exception error)
            {
                success = false;
                //_logger(ExceptionHelper.GetErrorText(error));
            }

            if (!success)
            {
                NackMessage(in consumer, in args);
            }
        }
        private void NackMessage(in EventingBasicConsumer consumer, in BasicDeliverEventArgs args)
        {
            try
            {
                Task.Delay(TimeSpan.FromSeconds(10), _token).Wait(_token);

                consumer.Model.BasicNack(args.DeliveryTag, false, true);
            }
            catch (Exception error)
            {
                //_logger(ExceptionHelper.GetErrorText(error));
            }
        }
        private Message CreateMessage(in BasicDeliverEventArgs args)
        {
            Message message = new();
            
            message.AppId = (args.BasicProperties.AppId ?? string.Empty);
            message.Type = (args.BasicProperties.Type ?? string.Empty);
            message.Body = (args.Body.IsEmpty ? string.Empty : Encoding.UTF8.GetString(args.Body.Span));
            
            message.Headers = args.BasicProperties.Headers;
            
            // TODO: process headers and other basic properties
            // string headers = GetMessageHeaders(in args);

            return message;
        }
        private string GetMessageHeaders(in BasicDeliverEventArgs args)
        {
            if (args.BasicProperties.Headers == null ||
                args.BasicProperties.Headers.Count == 0)
            {
                return string.Empty;
            }

            Dictionary<string, string> headers = new();

            foreach (var header in args.BasicProperties.Headers)
            {
                if (header.Value is byte[] value)
                {
                    try
                    {
                        headers.Add(header.Key, Encoding.UTF8.GetString(value));
                    }
                    catch
                    {
                        headers.Add(header.Key, string.Empty);
                    }
                }
            }

            if (headers.Count == 0)
            {
                return string.Empty;
            }

            return JsonSerializer.Serialize(headers);
        }
    }
}