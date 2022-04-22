namespace DaJet.Flow
{
    public sealed class PipelineOptions
    {
        public string Name { get; set; } = "DaJet Flow Pipeline";
        public bool IsActive { get; set; } = true;
        public int IdleTime { get; set; } = 60; // seconds
        public SourceOptions Source { get; set; } = new SourceOptions();
        public TargetOptions Target { get; set; } = new TargetOptions();
        public List<HandlerOptions> Handlers { get; set; } = new List<HandlerOptions>();
    }
    public sealed class SourceOptions
    {
        public string Type { get; set; } = string.Empty;
        public string Consumer { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
    public sealed class TargetOptions
    {
        public string Type { get; set; } = string.Empty;
        public string Producer { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
    public sealed class HandlerOptions
    {
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }
}