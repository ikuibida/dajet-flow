using DaJet.Flow;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace DaJet.SqlServer
{
    public sealed class Consumer<TMessage> : Source<TMessage> where TMessage : class, IMessage, new()
    {
        private readonly DatabaseOptions _options;
        private readonly IDataMapper<TMessage> _mapper;
        [ActivatorUtilitiesConstructor] public Consumer(DatabaseOptions options, IDataMapper<TMessage> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        public override void Pump(CancellationToken token)
        {
            TMessage message = new(); // TODO: readonly + message.Dispose()

            int consumed;
            
            using (SqlConnection connection = new(_options.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    _mapper.ConfigureSelect(command);

                    do
                    {
                        consumed = 0;

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
                    }
                    while (consumed > 0 && !token.IsCancellationRequested);
                }
            }
        }
    }
}