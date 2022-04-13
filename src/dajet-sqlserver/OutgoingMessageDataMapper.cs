using DaJet.Flow;
using DaJet.Flow.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DaJet.SqlServer.DataMappers
{
    public sealed class OutgoingMessageDataMapper : IDataMapper<OutgoingMessage>
    {
        private readonly DatabaseOptions _options;
        public OutgoingMessageDataMapper(DatabaseOptions options)
        {
            _options = options;
        }

        public void ConfigureSelect(in DbCommand command)
        {
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 60; // seconds
            command.CommandText = BuildSelectScript();

            command.Parameters.Clear();

            SqlParameter parameter = new SqlParameter("MessageCount", SqlDbType.Int)
            {
                Value = _options.MessagesPerTransaction
            };

            command.Parameters.Add(parameter);
        }
        public void MapDataToMessage(in DbDataReader reader, in OutgoingMessage message)
        {
            message.MessageNumber = reader.IsDBNull("МоментВремени") ? 0L : (long)reader.GetDecimal("МоментВремени");
            message.Uuid = reader.IsDBNull("Идентификатор") ? Guid.Empty : new Guid((byte[])reader["Идентификатор"]);
            message.Sender = reader.IsDBNull("Отправитель") ? string.Empty : reader.GetString("Отправитель");
            message.Recipients = reader.IsDBNull("Получатели") ? string.Empty : reader.GetString("Получатели");
            message.Headers = reader.IsDBNull("Заголовки") ? string.Empty : reader.GetString("Заголовки");
            message.MessageType = reader.IsDBNull("ТипСообщения") ? string.Empty : reader.GetString("ТипСообщения");
            message.MessageBody = reader.IsDBNull("ТелоСообщения") ? string.Empty : reader.GetString("ТелоСообщения");
            message.DateTimeStamp = reader.IsDBNull("ДатаВремя") ? DateTime.MinValue : reader.GetDateTime("ДатаВремя");
            message.Reference = reader.IsDBNull("Ссылка") ? Guid.Empty : new Guid((byte[])reader["Ссылка"]);
        }
        private string BuildSelectScript()
        {
            //string script =
            //    "WITH cte AS (SELECT TOP (@MessageCount) " +
            //    "{НомерСообщения} AS [MessageNumber], {Заголовки} AS [Headers], " +
            //    "{ТипСообщения} AS [MessageType], {ТелоСообщения} AS [MessageBody] " +
            //    "FROM {TABLE_NAME} WITH (ROWLOCK, READPAST) " +
            //    "ORDER BY {НомерСообщения} ASC) " +
            //    "DELETE cte OUTPUT " +
            //    "deleted.[MessageNumber], deleted.[Headers], " +
            //    "deleted.[MessageType], deleted.[MessageBody];";

            string script =
                "WITH cte AS (SELECT TOP (@MessageCount) " +
                "{МоментВремени} AS [МоментВремени], {Идентификатор} AS [Идентификатор], {Заголовки} AS [Заголовки], " +
                "{Отправитель} AS [Отправитель], {Получатели} AS [Получатели], " +
                "{ТипСообщения} AS [ТипСообщения], {ТелоСообщения} AS [ТелоСообщения], " +
                "{ДатаВремя} AS [ДатаВремя], {Ссылка} AS [Ссылка] " +
                "FROM {TABLE_NAME} WITH (ROWLOCK, READPAST) " +
                "ORDER BY {МоментВремени} ASC, {Идентификатор} ASC) " +
                "DELETE cte OUTPUT " +
                "deleted.[МоментВремени], deleted.[Идентификатор], deleted.[Заголовки], " +
                "deleted.[Отправитель], deleted.[Получатели], " +
                "deleted.[ТипСообщения], deleted.[ТелоСообщения], " +
                "deleted.[ДатаВремя], deleted.[Ссылка];";

            script = script.Replace("{TABLE_NAME}", _options.QueueTable);

            foreach (var column in _options.TableColumns)
            {
                script = script.Replace($"{{{column.Key}}}", column.Value);
            }

            return script;
        }

        public void ConfigureInsert(in DbCommand command, in OutgoingMessage message)
        {
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 10; // seconds
            command.CommandText = BuildInsertScript();

            command.Parameters.Clear();

            command.Parameters.Add(new SqlParameter("Заголовки", SqlDbType.NVarChar) { Value = message.Headers });
            command.Parameters.Add(new SqlParameter("Отправитель", SqlDbType.NVarChar) { Value = string.Empty });
            command.Parameters.Add(new SqlParameter("ТипСообщения", SqlDbType.NVarChar) { Value = message.MessageType });
            command.Parameters.Add(new SqlParameter("ТелоСообщения", SqlDbType.NVarChar) { Value = message.MessageBody });
            command.Parameters.Add(new SqlParameter("ДатаВремя", SqlDbType.DateTime2)
            {
                Value = DateTime.Now.AddYears(_options.YearOffset)
            });
            command.Parameters.Add(new SqlParameter("ОписаниеОшибки", SqlDbType.NVarChar) { Value = string.Empty });
            command.Parameters.Add(new SqlParameter("КоличествоОшибок", SqlDbType.Int) { Value = 0 });
        }
        private string BuildInsertScript()
        {
            string script =
                "INSERT {TABLE_NAME} " +
                "({НомерСообщения}, {Заголовки}, {Отправитель}, {ТипСообщения}, {ТелоСообщения}, {ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
                "SELECT NEXT VALUE FOR {SEQUENCE_NAME}, " +
                "@Заголовки, @Отправитель, @ТипСообщения, @ТелоСообщения, @ДатаВремя, @ОписаниеОшибки, @КоличествоОшибок;";

            script = script.Replace("{TABLE_NAME}", _options.QueueTable);
            script = script.Replace("{SEQUENCE_NAME}", _options.SequenceObject);

            foreach (var column in _options.TableColumns)
            {
                script = script.Replace($"{{{column.Key}}}", column.Value);
            }

            return script;
        }
    }
}