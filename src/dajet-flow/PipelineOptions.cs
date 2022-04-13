namespace DaJet.Flow
{
    public sealed class PipelineOptions
    {
        public string Name { get; set; } = "DaJet Flow Pipeline";
        public bool IsActive { get; set; } = true;
        public SourceOptions Source { get; set; } = new SourceOptions();
        public TargetOptions Target { get; set; } = new TargetOptions();
        public List<string> Handlers { get; set; } = new List<string>();
    }
    public sealed class SourceOptions
    {
        public string Type { get; set; } = string.Empty;
        public string Consumer { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DataMapper { get; set; } = string.Empty;
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
    public sealed class TargetOptions
    {
        public string Type { get; set; } = string.Empty;
        public string Producer { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DataMapper { get; set; } = string.Empty;
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
}