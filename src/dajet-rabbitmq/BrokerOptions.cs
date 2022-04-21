using System.Text;
using System.Web;

namespace DaJet.RabbitMQ
{
    public sealed class BrokerOptions
    {
        public string HostName { get; set; } = "localhost";
        public int HostPort { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string ExchangeName { get; set; } = string.Empty; // if exchange name is empty, then RoutingKey is a queue name to send directly
        public string RoutingKey { get; set; } = string.Empty; // if exchange name is not empty, then this is routing key value
        //public ExchangeRoles ExchangeRole { get; set; } = ExchangeRoles.None;
        public void ParseUri(string amqpUri)
        {
            // amqp://guest:guest@localhost:5672/%2F/РИБ.ERP

            Uri uri = new Uri(amqpUri);

            if (uri.Scheme != "amqp")
            {
                return;
            }

            HostName = uri.Host;
            HostPort = uri.Port;

            string[] userpass = uri.UserInfo.Split(':');
            if (userpass != null && userpass.Length == 2)
            {
                UserName = HttpUtility.UrlDecode(userpass[0], Encoding.UTF8);
                Password = HttpUtility.UrlDecode(userpass[1], Encoding.UTF8);
            }

            if (uri.Segments != null && uri.Segments.Length == 3)
            {
                if (uri.Segments.Length > 1)
                {
                    VirtualHost = HttpUtility.UrlDecode(uri.Segments[1].TrimEnd('/'), Encoding.UTF8);
                }

                if (uri.Segments.Length == 3)
                {
                    ExchangeName = HttpUtility.UrlDecode(uri.Segments[2].TrimEnd('/'), Encoding.UTF8);
                }
            }
        }
        public static BrokerOptions CreateOptions(Dictionary<string, string> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            BrokerOptions _options = new();

            if (options.TryGetValue(nameof(BrokerOptions.HostName), out string? HostName) && !string.IsNullOrWhiteSpace(HostName))
            {
                _options.HostName = HostName;
            }

            if (options.TryGetValue(nameof(BrokerOptions.HostPort), out string? HostPort) && !string.IsNullOrWhiteSpace(HostPort))
            {
                if (int.TryParse(HostPort, out int hostPort) && hostPort > 0)
                {
                    _options.HostPort = hostPort;
                }
            }

            if (options.TryGetValue(nameof(BrokerOptions.UserName), out string? UserName) && !string.IsNullOrWhiteSpace(UserName))
            {
                _options.UserName = UserName;
            }

            if (options.TryGetValue(nameof(BrokerOptions.Password), out string? Password) && !string.IsNullOrWhiteSpace(Password))
            {
                _options.Password = Password;
            }

            if (options.TryGetValue(nameof(BrokerOptions.VirtualHost), out string? VirtualHost) && !string.IsNullOrWhiteSpace(VirtualHost))
            {
                _options.VirtualHost = VirtualHost;
            }

            if (options.TryGetValue(nameof(BrokerOptions.ExchangeName), out string? ExchangeName) && !string.IsNullOrWhiteSpace(ExchangeName))
            {
                _options.ExchangeName = ExchangeName;
            }

            if (options.TryGetValue(nameof(BrokerOptions.RoutingKey), out string? RoutingKey) && !string.IsNullOrWhiteSpace(RoutingKey))
            {
                _options.RoutingKey = RoutingKey;
            }

            return _options;
        }
    }
}