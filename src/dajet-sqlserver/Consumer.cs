using DaJet.Flow;
using DaJet.Flow.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DaJet.SqlServer
{
    public sealed class Consumer<TMessage> : Source<TMessage>, IConfigurable where TMessage : class, IMessage, new()
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Consumer<TMessage>> _logger;

        private string? _connectionString;
        private IDataMapper<TMessage>? _mapper;

        [ActivatorUtilitiesConstructor] public Consumer(IPipeline pipeline)
        {
            _serviceProvider = pipeline.Services;

            _logger = _serviceProvider.GetRequiredService<ILogger<Consumer<TMessage>>>();

            //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            TMessage message = new(); // TODO: readonly + buffer + message.Dispose()

            int consumed;
            
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    _mapper!.ConfigureSelect(command);

                    do
                    {
                        consumed = 0;

                        Stopwatch watch = new Stopwatch();
                        watch.Start();

                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            command.Transaction = transaction;

                            using (SqlDataReader reader = command.ExecuteReader())
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
                        
                        _logger.LogInformation($"[SqlServer.Consumer] Consumed {consumed} messages in {watch.ElapsedMilliseconds} milliseconds.");
                    }
                    while (consumed > 0 && !token.IsCancellationRequested);
                }
            }
        }
    }
}