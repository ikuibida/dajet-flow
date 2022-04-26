namespace DaJet.Flow.Data
{
    public sealed class DataMapperOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int MessagesPerTransaction { get; set; } = 1000;
        public int YearOffset { get; set; } = 0;
        public string TableName { get; set; } = string.Empty;
        public string QueueObject { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public Dictionary<string, string> TableColumns { get; } = new Dictionary<string, string>();
    }
    public sealed class DataMapperOptionsBuilder : OptionsBuilder<DataMapperOptions>
    {
        public override DataMapperOptions Build(Dictionary<string, string> options)
        {
            if (!options.TryGetValue(nameof(DataMapperOptions.ConnectionString), out string? ConnectionString)
                || string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException($"Option \"{nameof(DataMapperOptions.ConnectionString)}\" is missing.");
            }

            DataMapperOptions mapperOptions = new()
            {
                ConnectionString = ConnectionString
            };

            if (options.TryGetValue(nameof(DataMapperOptions.MessagesPerTransaction), out string? MessagesPerTransaction)
                && !string.IsNullOrWhiteSpace(MessagesPerTransaction)
                && int.TryParse(MessagesPerTransaction, out int messagesPerTransaction))
            {
                mapperOptions.MessagesPerTransaction = (messagesPerTransaction > 0 ? messagesPerTransaction : 1000);
            }

            if (options.TryGetValue(nameof(DataMapperOptions.TableName), out string? TableName) && !string.IsNullOrWhiteSpace(TableName))
            {
                mapperOptions.TableName = TableName;
            }

            if (options.TryGetValue(nameof(DataMapperOptions.QueueObject), out string? QueueObject) && !string.IsNullOrWhiteSpace(QueueObject))
            {
                mapperOptions.QueueObject = QueueObject;
            }

            if (options.TryGetValue(nameof(DataMapperOptions.SequenceName), out string? SequenceName) && !string.IsNullOrWhiteSpace(SequenceName))
            {
                mapperOptions.SequenceName = SequenceName;
            }

            return mapperOptions;
        }
    }
}