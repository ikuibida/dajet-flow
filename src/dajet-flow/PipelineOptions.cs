namespace DaJet.Flow
{
    public sealed class PipelineOptions
    {
        public string Name { get; set; } = "DaJet Flow Pipeline";
        public SourceOptions Source { get; set; } = new SourceOptions();
        public TargetOptions Target { get; set; } = new TargetOptions();
        public List<string> Handlers { get; set; } = new List<string>(); // "DaJet.Flow.Contracts.Transformers.V1.OutgoingIncomingTransformer"
    }
    public sealed class SourceOptions
    {
        public string Type { get; set; } = "SqlServer";
        public string Consumer { get; set; } = "DaJet.SqlServer.Consumer`1";
        public string Message { get; set; } = "DaJet.Flow.Contracts.V1.OutgoingMessage";
        public string DataMapper { get; set; } = "DaJet.SqlServer.DataMappers.V1.OutgoingMessageDataMapper";
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
    public sealed class TargetOptions
    {
        public string Type { get; set; } = "SqlServer";
        public string Producer { get; set; } = "DaJet.SqlServer.Producer`1";
        public string Message { get; set; } = "DaJet.Flow.Contracts.V1.IncomingMessage";
        public string DataMapper { get; set; } = "DaJet.SqlServer.DataMappers.V1.IncomingMessageDataMapper";
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
}