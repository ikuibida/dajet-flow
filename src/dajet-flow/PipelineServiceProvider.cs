using Microsoft.Extensions.DependencyInjection;

namespace DaJet.Flow
{
    public sealed class PipelineServiceProvider : IServiceProvider, IDisposable
    {
        private ServiceProvider? _provider; // Pipeline service provider
        private readonly IServiceProvider _serviceProvider; // Host service provider
        private readonly ServiceCollection _services = new(); // Pipeline service collection
        public PipelineServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _services.AddSingleton(this);
        }
        public IServiceCollection Services { get { return _services; } }
        public void Dispose()
        {
            _services.Clear();
            _provider?.Dispose();
        }
        public object? GetService(Type serviceType)
        {
            if (_provider == null)
            {
                _provider = _services.BuildServiceProvider();
            }

            object? service = _provider.GetService(serviceType);

            if (service != null)
            {
                return service;
            }

            return _serviceProvider.GetService(serviceType);
        }
    }
}