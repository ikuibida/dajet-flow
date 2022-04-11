namespace DaJet.Flow
{
    public sealed class DatabaseOptions
    {
        public string DatabaseProvider { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public int YearOffset { get; set; } = 0;
        public int MessagesPerTransaction { get; set; } = 1000;

        // TODO: DataMapperOptions ?
        public string QueueObject { get; set; } = string.Empty;
        public string SequenceObject { get; set; } = string.Empty;
        public string QueueTable { get; set; } = string.Empty;
        public Dictionary<string, string> OrderColumns { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> TableColumns { get; } = new Dictionary<string, string>();
    }
}