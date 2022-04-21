using DaJet.Flow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace DaJet.RabbitMQ
{
    public sealed class Producer : Target<Message>
    {
        private readonly BrokerOptions _options;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Producer> _logger;

        private IModel? _channel;
        private IConnection? _connection;
        private IBasicProperties? _properties;
        private bool ConnectionIsBlocked = false;

        [ActivatorUtilitiesConstructor]
        public Producer(IServiceProvider serviceProvider, Dictionary<string, string> options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = BrokerOptions.CreateOptions(options);

            _logger = _serviceProvider.GetRequiredService<ILogger<Producer>>();
        }

        #region "RABBITMQ CONNECTION AND CHANNEL SETUP"

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

            _connection.ConnectionBlocked += HandleConnectionBlocked;
            _connection.ConnectionUnblocked += HandleConnectionUnblocked;
        }
        private void HandleConnectionBlocked(object sender, ConnectionBlockedEventArgs args)
        {
            ConnectionIsBlocked = true;
        }
        private void HandleConnectionUnblocked(object sender, EventArgs args)
        {
            ConnectionIsBlocked = false;
        }

        private void InitializeChannel()
        {
            if (_channel != null && _channel.IsOpen)
            {
                return;
            }

            _channel?.Dispose(); // The channel might be closed, but not disposed yet.

            InitializeConnection();

            _channel = _connection.CreateModel();
            _channel.ConfirmSelect();
            _properties = _channel.CreateBasicProperties();

            _channel.BasicAcks += BasicAcksHandler;
            _channel.BasicNacks += BasicNacksHandler;
        }
        private void BasicAcksHandler(object sender, BasicAckEventArgs args)
        {
            //if (!(sender is IModel channel)) return;
            //_deliveryTag = args.DeliveryTag;
        }
        private void BasicNacksHandler(object sender, BasicNackEventArgs args)
        {
            //if (args.DeliveryTag <= _deliveryTag)
            //{
            //    _nacked = true;
            //}
        }

        #endregion

        private void ThrowIfConnectionIsBlocked()
        {
            if (!ConnectionIsBlocked)
            {
                return;
            }

            _Dispose();

            throw new Exception("Connection is blocked by broker.");
        }
        private void ThrowIfChannelIsNotHealthy()
        {
            try
            {
                InitializeChannel();
            }
            catch
            {
                _Dispose();

                throw;
            }
        }

        protected override void _Synchronize()
        {
            if (_channel == null)
            {
                throw new NullReferenceException(nameof(_channel));
            }

            if (_channel.WaitForConfirms())
            {
                return;
            }

            _Dispose();

            throw new InvalidOperationException(nameof(_Synchronize));
        }
        protected override void _Dispose()
        {
            _properties = null;

            _channel?.Dispose();
            _channel = null;

            _connection?.Dispose();
            _connection = null;
        }

        protected override void _Process(in Message message)
        {
            ThrowIfConnectionIsBlocked();

            ThrowIfChannelIsNotHealthy();

            CopyMessageProperties(in message);

            ReadOnlyMemory<byte> messageBody = Encoding.UTF8.GetBytes(message.Body);

            if (string.IsNullOrWhiteSpace(_options.RoutingKey))
            {
                _channel.BasicPublish(_options.ExchangeName, message.Type, _properties, messageBody);
            }
            else
            {
                _channel.BasicPublish(_options.ExchangeName, _options.RoutingKey, _properties, messageBody);
            }
        }
        private void CopyMessageProperties(in Message message)
        {
            _properties.Type = message.Type;
            _properties.Headers = message.Headers;

            _properties.DeliveryMode = message.DeliveryMode;
            _properties.ContentType = message.ContentType;
            _properties.ContentEncoding = message.ContentEncoding;

            _properties.AppId = message.AppId;
            _properties.UserId = message.UserId;
            _properties.ClusterId = message.ClusterId;
            _properties.MessageId = message.MessageId;

            _properties.Priority = message.Priority;
            _properties.Expiration = message.Expiration;

            _properties.ReplyTo = message.ReplyTo;
            _properties.CorrelationId = message.CorrelationId;
        }
    }
}