using DaJet.Flow;
using DaJet.Flow.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DaJet.SqlServer.DataMappers
{
    public sealed class IncomingMessageDataMapper : IDataMapper<IncomingMessage>
    {
        private readonly DatabaseOptions _options;
        public IncomingMessageDataMapper(DatabaseOptions options)
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
        public void MapDataToMessage(in DbDataReader reader, in IncomingMessage message)
        {
            message.MessageNumber = reader.IsDBNull("MessageNumber") ? 0L : (long)reader.GetDecimal("MessageNumber");
            message.Headers = reader.IsDBNull("Headers") ? string.Empty : reader.GetString("Headers");
            message.MessageType = reader.IsDBNull("MessageType") ? string.Empty : reader.GetString("MessageType");
            message.MessageBody = reader.IsDBNull("MessageBody") ? string.Empty : reader.GetString("MessageBody");
        }
        private string BuildSelectScript()
        {
            string script =
                "WITH cte AS (SELECT TOP (@MessageCount) " +
                "{НомерСообщения} AS [MessageNumber], {Заголовки} AS [Headers], " +
                "{ТипСообщения} AS [MessageType], {ТелоСообщения} AS [MessageBody] " +
                "FROM {TABLE_NAME} WITH (ROWLOCK, READPAST) " +
                "ORDER BY {НомерСообщения} ASC) " +
                "DELETE cte OUTPUT " +
                "deleted.[MessageNumber], deleted.[Headers], " +
                "deleted.[MessageType], deleted.[MessageBody];";

            script = script.Replace("{TABLE_NAME}", _options.QueueTable);

            foreach (var column in _options.TableColumns)
            {
                if (column.Key == "НомерСообщения")
                {
                    script = script.Replace("{НомерСообщения}", column.Value);
                }
                else if (column.Key == "Заголовки")
                {
                    script = script.Replace("{Заголовки}", column.Value);
                }
                else if (column.Key == "ТипСообщения")
                {
                    script = script.Replace("{ТипСообщения}", column.Value);
                }
                else if (column.Key == "ТелоСообщения")
                {
                    script = script.Replace("{ТелоСообщения}", column.Value);
                }
            }

            return script;
        }

        public void ConfigureInsert(in DbCommand command, in IncomingMessage message)
        {
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 10; // seconds
            command.CommandText = BuildInsertScript();

            command.Parameters.Clear();

            command.Parameters.Add(new SqlParameter("МоментВремени", SqlDbType.Decimal) { Value = message.MessageNumber });
            command.Parameters.Add(new SqlParameter("Идентификатор", SqlDbType.Binary) { Value = message.Uuid.ToByteArray() });
            command.Parameters.Add(new SqlParameter("Заголовки", SqlDbType.NVarChar) { Value = message.Headers });
            command.Parameters.Add(new SqlParameter("Отправитель", SqlDbType.NVarChar) { Value = message.Sender });
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
            //string script =
            //    "INSERT {TABLE_NAME} " +
            //    "({НомерСообщения}, {Заголовки}, {Отправитель}, {ТипСообщения}, {ТелоСообщения}, {ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
            //    "SELECT NEXT VALUE FOR {SEQUENCE_NAME}, " +
            //    "@Заголовки, @Отправитель, @ТипСообщения, @ТелоСообщения, @ДатаВремя, @ОписаниеОшибки, @КоличествоОшибок;";

            string script =
                "INSERT {TABLE_NAME} " +
                "({МоментВремени}, {Идентификатор}, {Заголовки}, {Отправитель}, {ТипСообщения}, {ТелоСообщения}, {ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
                "SELECT @МоментВремени, @Идентификатор, " +
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