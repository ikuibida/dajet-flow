using DaJet.Flow;
using DaJet.Flow.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;

namespace DaJet.PostgreSQL
{
    public sealed class Consumer<TMessage> : Source<TMessage>, IConfigurable where TMessage : class, IMessage, new()
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Consumer<TMessage>> _logger;

        private string? _connectionString;
        private IDataMapper<TMessage>? _mapper;

        [ActivatorUtilitiesConstructor] public Consumer(IPipeline pipeline)
        {
            _serviceProvider = pipeline.HostServices;

            _logger = _serviceProvider.GetRequiredService<ILogger<Consumer<TMessage>>>();
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
        public override void Pump(CancellationToken token)
        {
            TMessage message = new();

            int consumed;

            using (NpgsqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    _mapper!.ConfigureSelect(command);

                    do
                    {
                        consumed = 0;

                        Stopwatch watch = new Stopwatch();
                        watch.Start();

                        using (NpgsqlTransaction transaction = connection.BeginTransaction())
                        {
                            command.Transaction = transaction;

                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    consumed++;

                                    _mapper.MapDataToMessage(reader, in message);

                                    _Process(in message);
                                }
                                reader.Close();
                            }

                            if (consumed > 0)
                            {
                                _Synchronize();

                                transaction.Commit();
                            }
                        }

                        watch.Stop();
                        if (consumed > 0)
                        {
                            _logger.LogInformation($"[PostgreSQL.Consumer] Consumed {consumed} messages in {watch.ElapsedMilliseconds} milliseconds.");
                        }
                    }
                    while (consumed > 0 && !token.IsCancellationRequested);
                }
            }
        }
    }
}