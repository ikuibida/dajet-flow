using DaJet.Flow;
using DaJet.Flow.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DaJet.SqlServer
{
    public sealed class Producer<TMessage> : Target<TMessage>, IConfigurable where TMessage : class, IMessage, new()
    {
        private ILogger? _logger;
        private readonly IServiceProvider _serviceProvider;
        
        private string? _connectionString;
        private IDataMapper<TMessage>? _mapper;

        [ActivatorUtilitiesConstructor] public Producer(IPipeline pipeline)
        {
            _serviceProvider = pipeline.Services;
        }
        public void Configure(Dictionary<string, string> options)
        {
            _logger = _serviceProvider.GetService<ILogger<Producer<TMessage>>>();

            DataMapperOptions mapperOptions = _serviceProvider
                .GetRequiredService<DataMapperOptionsBuilder>()
                .Build(options);

            _connectionString = mapperOptions.ConnectionString;

            _mapper = _serviceProvider
                .GetRequiredService<DataMapperFactory>()
                .CreateDataMapper<TMessage>(mapperOptions);
        }
        protected override void _Process(in TMessage message)
        {
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    _mapper!.ConfigureInsert(command, in message);

                    _ = command.ExecuteNonQuery();
                }
            }
        }
    }
}