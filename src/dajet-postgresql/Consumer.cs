using DaJet.Flow;
using DaJet.Metadata;
using DaJet.Metadata.Model;
using DaJet.PostgreSQL.DataMappers;
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

        private readonly DatabaseProvider _databaseProvider = DatabaseProvider.PostgreSQL;

        [ActivatorUtilitiesConstructor] public Consumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _logger = _serviceProvider.GetRequiredService<ILogger<Consumer<TMessage>>>();
        }
        public void Configure(Dictionary<string, string> options)
        {
            DataMapperOptions mapperOptions = CreateDataMapperOptions(options);
            
            _mapper = CreateDataMapper(mapperOptions) as IDataMapper<TMessage>;
        }
        private DataMapperOptions CreateDataMapperOptions(Dictionary<string, string> options)
        {
            if (!options.TryGetValue("ConnectionString", out _connectionString) || string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Database connection string is missing.");
            }

            DataMapperOptions mapperOptions = new DataMapperOptions();

            if (options.TryGetValue(nameof(DataMapperOptions.MessagesPerTransaction), out string? messagesPerTransaction)
                && !string.IsNullOrWhiteSpace(messagesPerTransaction)
                && int.TryParse(messagesPerTransaction, out int parameterValue))
            {
                mapperOptions.MessagesPerTransaction = (parameterValue > 0 ? parameterValue : 1000);
            }

            if (!options.TryGetValue(nameof(DataMapperOptions.QueueObject), out string? queueObject)
                || string.IsNullOrWhiteSpace(queueObject))
            {
                return mapperOptions;
            }

            mapperOptions.QueueObject = queueObject;

            IMetadataService metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

            metadataService.UseDatabaseProvider(_databaseProvider).UseConnectionString(_connectionString);

            if (!metadataService.TryOpenInfoBase(out InfoBase infoBase, out string error))
            {
                throw new InvalidOperationException(error);
            }

            mapperOptions.YearOffset = infoBase.YearOffset;

            ApplicationObject queue = infoBase.GetApplicationObjectByName(queueObject);

            if (queue == null)
            {
                throw new InvalidOperationException($"Queue object [{queueObject}] is not found.");
            }

            mapperOptions.TableName = queue.TableName;
            mapperOptions.SequenceName = queue.TableName + "_so";

            foreach (MetadataProperty property in queue.Properties)
            {
                if (property.Fields != null && property.Fields.Count == 1)
                {
                    DatabaseField field = property.Fields[0];

                    mapperOptions.TableColumns.Add(property.Name, field.Name);
                }
            }

            return mapperOptions;
        }
        private object CreateDataMapper(DataMapperOptions options)
        {
            Type mapperType = typeof(OutgoingMessageDataMapper);

            return ActivatorUtilities.CreateInstance(_serviceProvider, mapperType, options);
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