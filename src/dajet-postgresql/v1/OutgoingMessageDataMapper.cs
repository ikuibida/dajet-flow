using DaJet.Flow;
using DaJet.Flow.Contracts.V1;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DaJet.PostgreSQL.DataMappers.V1
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

            NpgsqlParameter parameter = new NpgsqlParameter("MessageCount", NpgsqlDbType.Integer)
            {
                Value = _options.MessagesPerTransaction
            };

            command.Parameters.Add(parameter);
        }
        public void MapDataToMessage(in DbDataReader reader, in OutgoingMessage message)
        {
            message.MessageNumber = reader.IsDBNull("НомерСообщения") ? 0L : (long)reader.GetDecimal("НомерСообщения");
            message.Uuid = reader.IsDBNull("Идентификатор") ? Guid.Empty : new Guid((byte[])reader["Идентификатор"]);
            message.Headers = reader.IsDBNull("Заголовки") ? string.Empty : reader.GetString("Заголовки");
            message.MessageType = reader.IsDBNull("ТипСообщения") ? string.Empty : reader.GetString("ТипСообщения");
            message.MessageBody = reader.IsDBNull("ТелоСообщения") ? string.Empty : reader.GetString("ТелоСообщения");
            message.DateTimeStamp = reader.IsDBNull("ДатаВремя") ? DateTime.MinValue : reader.GetDateTime("ДатаВремя");
            message.Reference = reader.IsDBNull("Ссылка") ? Guid.Empty : new Guid((byte[])reader["Ссылка"]);
        }
        private string BuildSelectScript()
        {
            string script =
                "WITH cte AS (SELECT {НомерСообщения}, {Идентификатор} "+
                "FROM {TABLE_NAME} ORDER BY {НомерСообщения} ASC, {Идентификатор} ASC LIMIT @MessageCount) " +
                "DELETE FROM {TABLE_NAME} t USING cte " +
                "WHERE t.{НомерСообщения} = cte.{НомерСообщения} AND t.{Идентификатор} = cte.{Идентификатор} " +
                "RETURNING t.{НомерСообщения} AS \"НомерСообщения\", t.{Идентификатор} AS \"Идентификатор\", "+
                "CAST(t.{Заголовки} AS text) AS \"Заголовки\", " +
                "CAST(t.{ТипСообщения} AS varchar) AS \"ТипСообщения\", CAST(t.{ТелоСообщения} AS text) AS \"ТелоСообщения\", " +
                "t.{Ссылка} AS \"Ссылка\", t.{ДатаВремя} AS \"ДатаВремя\";";

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

            command.Parameters.Add(new NpgsqlParameter("Заголовки", NpgsqlDbType.Varchar) { Value = message.Headers });
            command.Parameters.Add(new NpgsqlParameter("Отправитель", NpgsqlDbType.Varchar) { Value = string.Empty });
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
            string script =
                "INSERT INTO {TABLE_NAME} " +
                "({НомерСообщения}, {Заголовки}, {Отправитель}, {ТипСообщения}, " +
                "{ТелоСообщения}, {ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
                "SELECT CAST(nextval('{SEQUENCE_NAME}') AS numeric(19,0)), " +
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