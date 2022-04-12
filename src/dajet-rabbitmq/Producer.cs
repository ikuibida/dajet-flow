using DaJet.Flow;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Buffers;
using System.Text;

namespace DaJet.RabbitMQ
{
    public sealed class Producer : Target<Message>
    {
        private readonly BrokerOptions _options;

        private IModel? _channel;
        private IConnection? _connection;
        private IBasicProperties? _properties;
        private bool ConnectionIsBlocked = false;

        private byte[]? _buffer; // message body buffer

        [ActivatorUtilitiesConstructor] public Producer(Dictionary<string, string> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = new BrokerOptions();

            if (options.TryGetValue(nameof(BrokerOptions.HostName), out string HostName))
            {
                _options.HostName = HostName;
            }

            if (options.TryGetValue(nameof(BrokerOptions.HostPort), out string HostPort))
            {
                _options.HostPort = int.Parse(HostPort);
            }

            if (options.TryGetValue(nameof(BrokerOptions.UserName), out string UserName))
            {
                _options.UserName = UserName;
            }

            if (options.TryGetValue(nameof(BrokerOptions.Password), out string Password))
            {
                _options.Password = Password;
            }

            if (options.TryGetValue(nameof(BrokerOptions.VirtualHost), out string VirtualHost))
            {
                _options.VirtualHost = VirtualHost;
            }

            if (options.TryGetValue(nameof(BrokerOptions.ExchangeName), out string ExchangeName))
            {
                _options.ExchangeName = ExchangeName;
            }

            if (options.TryGetValue(nameof(BrokerOptions.RoutingKey), out string RoutingKey))
            {
                _options.RoutingKey = RoutingKey;
            }
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
            InitializeConnection();

            if (_channel != null && _channel.IsOpen)
            {
                return;
            }

            _channel?.Dispose(); // The channel might be closed, but not disposed yet.

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

            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }
        }

        protected override void _Process(in Message message)
        {
            ThrowIfConnectionIsBlocked();

            ThrowIfChannelIsNotHealthy();

            CopyMessageProperties(in message);

            ReadOnlyMemory<byte> messageBody = GetMessageBody(in message);

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
        private ReadOnlyMemory<byte> GetMessageBody(in Message message)
        {
            int bufferSize = message.Body.Length * 2; // char == 2 bytes

            if (_buffer == null)
            {
                _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            }
            else if (_buffer.Length < bufferSize)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            }

            int encoded = Encoding.UTF8.GetBytes(message.Body, 0, message.Body.Length, _buffer, 0);

            ReadOnlyMemory<byte> messageBody = new ReadOnlyMemory<byte>(_buffer, 0, encoded);

            return messageBody;
        }
    }
}