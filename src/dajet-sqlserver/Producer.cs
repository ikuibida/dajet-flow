using DaJet.Flow;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace DaJet.SqlServer
{
    public sealed class Producer<TMessage> : Target<TMessage> where TMessage : class, IMessage, new()
    {
        private readonly DatabaseOptions _options;
        private readonly IDataMapper<TMessage> _mapper;
        [ActivatorUtilitiesConstructor] public Producer(DatabaseOptions options, IDataMapper<TMessage> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        protected override void _Process(in TMessage message)
        {
            using (SqlConnection connection = new(_options.ConnectionString))
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