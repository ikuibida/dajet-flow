using DaJet.Flow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;

namespace DaJet.PostgreSQL
{
    public sealed class Consumer<TMessage> : Source<TMessage> where TMessage : class, IMessage, new()
    {
        private readonly IDataMapper<TMessage> _mapper;
        private readonly Dictionary<string, string> _options;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Consumer<TMessage>> _logger;

        private readonly string? _connectionString;

        [ActivatorUtilitiesConstructor]
        public Consumer(IServiceProvider serviceProvider, Dictionary<string, string> options, IDataMapper<TMessage> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _logger = _serviceProvider.GetRequiredService<ILogger<Consumer<TMessage>>>();

            if (!_options.TryGetValue("ConnectionString", out _connectionString) || string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new ArgumentException("ConnectionString");
            }
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
                    _mapper.ConfigureSelect(command);

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