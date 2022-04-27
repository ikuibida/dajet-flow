using Microsoft.Extensions.DependencyInjection;

namespace DaJet.Flow
{
    public interface IPipeline
    {
        PipelineOptions Options { get; }
        void Configure(PipelineOptions options);
        void ConfigureServices(Action<IServiceCollection> configure);
        Dictionary<string, object> Variables { get; }
        IServiceProvider Services { get; }
        IServiceProvider HostServices { get; }
        void Run();
        //void Suspend();
        //void Continue();
        //void Close(); Dispose()
    }
    public sealed class Pipeline<T> : IPipeline
    {
        private readonly Dictionary<string, object> _context = new();
        private readonly CancellationTokenSource _cancellation = new();
        public Pipeline(IServiceProvider serviceProvider)
        {
            HostServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public PipelineOptions Options { get; private set; }
        public IServiceProvider Services { get; private set; }
        public IServiceProvider HostServices { get; private set; }
        public Dictionary<string, object> Variables { get { return _context; } }
        public void Configure(PipelineOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            ServiceCollection services = new();
            services.AddSingleton<IPipeline>(this);
            Services = services.BuildServiceProvider();
        }
        public void ConfigureServices(Action<IServiceCollection> configure)
        {
            ServiceCollection services = new();
            services.AddSingleton<IPipeline>(this);
            configure(services);
            Services = services.BuildServiceProvider();
        }
        public void Run()
        {
            Source<T> source = Services.GetRequiredService<Source<T>>();

            using (source)
            {
                source.Pump(_cancellation.Token);
            }
        }
    }
}