using DaJet.Flow;
using DaJet.Flow.Contracts;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DaJet.PostgreSQL.DataMappers
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

            NpgsqlParameter parameter = new NpgsqlParameter("MessageCount", SqlDbType.Int)
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

            command.Parameters.Add(new NpgsqlParameter("МоментВремени", NpgsqlDbType.Numeric) { Value = message.MessageNumber });
            command.Parameters.Add(new NpgsqlParameter("Идентификатор", NpgsqlDbType.Bytea) { Value = message.Uuid.ToByteArray() });
            command.Parameters.Add(new NpgsqlParameter("Заголовки", NpgsqlDbType.Varchar) { Value = message.Headers });
            command.Parameters.Add(new NpgsqlParameter("Отправитель", NpgsqlDbType.Varchar) { Value = message.Sender });
            command.Parameters.Add(new NpgsqlParameter("ТипСообщения", NpgsqlDbType.Varchar) { Value = message.MessageType });
            command.Parameters.Add(new NpgsqlParameter("ТелоСообщения", NpgsqlDbType.Varchar) { Value = message.MessageBody });
            command.Parameters.Add(new NpgsqlParameter("ДатаВремя", NpgsqlDbType.Timestamp)
            {
                Value = DateTime.Now.AddYears(_options.YearOffset)
            });
            command.Parameters.Add(new NpgsqlParameter("ОписаниеОшибки", NpgsqlDbType.Varchar) { Value = string.Empty });
            command.Parameters.Add(new NpgsqlParameter("КоличествоОшибок", NpgsqlDbType.Integer) { Value = 0 });
        }
        private string BuildInsertScript()
        {
            //string script =
            //    "INSERT INTO {TABLE_NAME} " +
            //    "({НомерСообщения}, {Заголовки}, {Отправитель}, {ТипСообщения}, {ТелоСообщения}, " +
            //    "{ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
            //    "SELECT CAST(nextval('{SEQUENCE_NAME}') AS numeric(19,0)), " +
            //    "CAST(@Заголовки AS mvarchar), CAST(@Отправитель AS mvarchar), CAST(@ТипСообщения AS mvarchar), " +
            //    "CAST(@ТелоСообщения AS mvarchar), @ДатаВремя, CAST(@ОписаниеОшибки AS mvarchar), @КоличествоОшибок;";

            string script =
                "INSERT INTO {TABLE_NAME} " +
                "({МоментВремени}, {Идентификатор}, {Заголовки}, {Отправитель}, {ТипСообщения}, {ТелоСообщения}, " +
                "{ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
                "SELECT @МоментВремени, @Идентификатор, " +
                "CAST(@Заголовки AS mvarchar), CAST(@Отправитель AS mvarchar), CAST(@ТипСообщения AS mvarchar), " +
                "CAST(@ТелоСообщения AS mvarchar), @ДатаВремя, CAST(@ОписаниеОшибки AS mvarchar), @КоличествоОшибок;";

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