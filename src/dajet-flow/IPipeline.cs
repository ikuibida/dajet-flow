using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DaJet.Flow
{
    public interface IPipeline : IDisposable
    {
        PipelineOptions Options { get; }
        IServiceProvider Services { get; }
        Dictionary<string, object> Context { get; }
        void Run();
        //void Suspend();
        //void Continue();
        //void Close();
    }
    public sealed class Pipeline<T> : IPipeline
    {
        private bool _disposed = false;
        private readonly IOptions<PipelineOptions> _options;
        private readonly Dictionary<string, object> _context = new();
        private readonly CancellationTokenSource _cancellation = new();
        public Pipeline(IOptions<PipelineOptions> options, PipelineServiceProvider serviceProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.Value == null) throw new ArgumentNullException(nameof(options.Value));
            Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public PipelineOptions Options { get { return _options.Value; } }
        public IServiceProvider Services { get; private set; }
        public Dictionary<string, object> Context { get { return _context; } }
        public void Run()
        {
            ISource<T> source = Services.GetRequiredService<ISource<T>>();

            using (source)
            {
                source.Pump(_cancellation.Token);
            }
        }
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            _disposed = true; // See comment below

            try
            {
                Context.Clear();

                // Service provider invokes Dispose method on all services
                // in the container, including this pipeline service instance.

                (Services as PipelineServiceProvider)?.Dispose();
            }
            catch
            {
                // do nothing
            }
        }
    }
}