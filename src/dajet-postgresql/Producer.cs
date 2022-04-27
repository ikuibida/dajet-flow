using DaJet.Flow;
using DaJet.Flow.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DaJet.PostgreSQL
{
    public sealed class Producer<TMessage> : Target<TMessage>, IConfigurable where TMessage : class, IMessage, new()
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Producer<TMessage>> _logger;

        private string? _connectionString;
        private IDataMapper<TMessage>? _mapper;

        [ActivatorUtilitiesConstructor] public Producer(IPipeline pipeline)
        {
            _serviceProvider = pipeline.Services;

            _logger = _serviceProvider.GetRequiredService<ILogger<Producer<TMessage>>>();
        }
        public void Configure(Dictionary<string, string> options)
        {
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
            using (NpgsqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    _mapper!.ConfigureInsert(command, in message);

                    _ = command.ExecuteNonQuery();
                }
            }
        }
    }
}