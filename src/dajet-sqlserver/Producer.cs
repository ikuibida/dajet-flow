using DaJet.Flow;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DaJet.SqlServer
{
    public sealed class Producer<TMessage> : Target<TMessage> where TMessage : class, IMessage, new()
    {
        private readonly IDataMapper<TMessage> _mapper;
        private readonly Dictionary<string, string> _options;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Producer<TMessage>> _logger;

        private readonly string? _connectionString;

        [ActivatorUtilitiesConstructor]
        public Producer(IServiceProvider serviceProvider, Dictionary<string, string> options, IDataMapper<TMessage> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _logger = _serviceProvider.GetRequiredService<ILogger<Producer<TMessage>>>();

            if (!_options.TryGetValue("ConnectionString", out _connectionString) || string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new ArgumentException("ConnectionString");
            }
        }
        protected override void _Process(in TMessage message)
        {
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    _mapper.ConfigureInsert(command, in message);

                    _ = command.ExecuteNonQuery();
                }
            }
        }
    }
}