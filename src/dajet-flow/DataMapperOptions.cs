namespace DaJet.Flow
{
    public sealed class DataMapperOptions
    {
        public int YearOffset { get; set; } = 0;
        public int MessagesPerTransaction { get; set; } = 1000;
        public string TableName { get; set; } = string.Empty;
        public string QueueObject { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public Dictionary<string, string> TableColumns { get; } = new Dictionary<string, string>();
    }
}