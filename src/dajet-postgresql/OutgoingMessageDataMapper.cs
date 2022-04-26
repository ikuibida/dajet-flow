using DaJet.Flow.Contracts;
using DaJet.Flow.Data;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DaJet.PostgreSQL.DataMappers
{
    public sealed class OutgoingMessageDataMapper : IDataMapper<OutgoingMessage>
    {
        private DataMapperOptions? _options;
        public void Configure(DataMapperOptions options)
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
            message.MessageNumber = reader.IsDBNull("МоментВремени") ? 0L : (long)reader.GetDecimal("МоментВремени");
            message.Uuid = reader.IsDBNull("Идентификатор") ? Guid.Empty : new Guid((byte[])reader["Идентификатор"]);
            message.Sender = reader.IsDBNull("Отправитель") ? string.Empty : reader.GetString("Отправитель");
            message.Recipients = reader.IsDBNull("Получатели") ? string.Empty : reader.GetString("Получатели");
            message.Headers = reader.IsDBNull("Заголовки") ? string.Empty : reader.GetString("Заголовки");
            message.MessageType = reader.IsDBNull("ТипСообщения") ? string.Empty : reader.GetString("ТипСообщения");
            message.MessageBody = reader.IsDBNull("ТелоСообщения") ? string.Empty : reader.GetString("ТелоСообщения");
            message.DateTimeStamp = reader.IsDBNull("ДатаВремя") ? DateTime.MinValue : reader.GetDateTime("ДатаВремя");
        }
        private string BuildSelectScript()
        {
            string script =
                "WITH cte AS (SELECT {МоментВремени}, {Идентификатор} "+
                "FROM {TABLE_NAME} ORDER BY {МоментВремени} ASC, {Идентификатор} ASC LIMIT @MessageCount) " +
                "DELETE FROM {TABLE_NAME} t USING cte " +
                "WHERE t.{МоментВремени} = cte.{МоментВремени} AND t.{Идентификатор} = cte.{Идентификатор} " +
                "RETURNING t.{МоментВремени} AS \"МоментВремени\", t.{Идентификатор} AS \"Идентификатор\", " +
                "CAST(t.{Отправитель} AS text) AS \"Отправитель\", CAST(t.{Получатели} AS text) AS \"Получатели\", " +
                "CAST(t.{Заголовки} AS text) AS \"Заголовки\", " +
                "CAST(t.{ТипСообщения} AS varchar) AS \"ТипСообщения\", CAST(t.{ТелоСообщения} AS text) AS \"ТелоСообщения\", " +
                "t.{ДатаВремя} AS \"ДатаВремя\";";

            script = script.Replace("{TABLE_NAME}", _options.TableName);

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

            command.Parameters.Add(new NpgsqlParameter("МоментВремени", NpgsqlDbType.Numeric) { Value = message.MessageNumber });
            command.Parameters.Add(new NpgsqlParameter("Идентификатор", NpgsqlDbType.Bytea) { Value = message.Uuid.ToByteArray() });
            command.Parameters.Add(new NpgsqlParameter("Заголовки", NpgsqlDbType.Varchar) { Value = message.Headers });
            command.Parameters.Add(new NpgsqlParameter("Отправитель", NpgsqlDbType.Varchar) { Value = string.Empty });
            command.Parameters.Add(new NpgsqlParameter("Получатели", NpgsqlDbType.Varchar) { Value = string.Empty });
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
                "({МоментВремени}, {Идентификатор}, {Заголовки}, {Отправитель}, {Получатели}, {ТипСообщения}, " +
                "{ТелоСообщения}, {ДатаВремя}, {ОписаниеОшибки}, {КоличествоОшибок}) " +
                "SELECT @МоментВремени, @Идентификатор" +
                "CAST(@Заголовки AS mvarchar), CAST(@Отправитель AS mvarchar), CAST(@Получатели AS mvarchar), CAST(@ТипСообщения AS mvarchar), " +
                "CAST(@ТелоСообщения AS mvarchar), @ДатаВремя, CAST(@ОписаниеОшибки AS mvarchar), @КоличествоОшибок;";

            script = script.Replace("{TABLE_NAME}", _options.TableName);
            script = script.Replace("{SEQUENCE_NAME}", _options.SequenceName);

            foreach (var column in _options.TableColumns)
            {
                script = script.Replace($"{{{column.Key}}}", column.Value);
            }

            return script;
        }
    }
}