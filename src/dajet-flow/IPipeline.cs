namespace DaJet.Flow
{
    public interface IPipeline
    {
        PipelineOptions Options { get; }
        void Execute();
        //void Suspend();
        //void Continue();
        //void Close(); Dispose()
    }
    public sealed class Pipeline<TSource> : IPipeline
    {
        private readonly PipelineOptions _options;
        private readonly ISource<TSource> _source;
        private readonly CancellationTokenSource _cancellation = new();
        public Pipeline(PipelineOptions options, ISource<TSource> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source)); ;
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        public PipelineOptions Options { get { return _options; } }
        public void Execute()
        {
            using (_source)
            {
                _source.Pump(_cancellation.Token);
            }
        }
    }
}