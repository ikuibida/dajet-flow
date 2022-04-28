using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DaJet.Flow
{
    public interface IPipelineBuilder
    {
        IPipelineBuilder Configure(PipelineOptions options);
        IPipeline Build();
    }
    public sealed class PipelineBuilder : IPipelineBuilder
    {
        private PipelineOptions? _options;

        private readonly ILogger<PipelineBuilder> _logger;
        private readonly IServiceProvider _serviceProvider;
        public PipelineBuilder(IServiceProvider serviceProvider, ILogger<PipelineBuilder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public IPipelineBuilder Configure(PipelineOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            return this;
        }
        public IPipeline Build()
        {
            if (_options == null)
            {
                throw new InvalidOperationException("Pipeline options are not provided.");
            }

            PipelineServiceProvider serviceProvider = _serviceProvider.GetRequiredService<PipelineServiceProvider>();

            Type sourceType = ResolveServiceType(_options.Source.Type, _options.Source.Message);
            Type messageType = GetSourceMessageType(sourceType);
            Type pipelineType = typeof(Pipeline<>).MakeGenericType(messageType);
            Type sourceInterfaceType = typeof(ISource<>).MakeGenericType(messageType);

            serviceProvider.Services.AddOptions();
            serviceProvider.Services.AddSingleton(Options.Create(_options));
            serviceProvider.Services.AddSingleton(typeof(IPipeline), pipelineType);
            serviceProvider.Services.AddSingleton(sourceInterfaceType, sourceType);

            //IPipeline pipeline = serviceProvider.GetRequiredService<IPipeline>();

            try
            {
                List<object> services = CreatePipelineServices(sourceInterfaceType, serviceProvider);
                
                object source = AssemblePipeline(in services);
            }
            catch (Exception error)
            {
                _logger.LogError($"[{_options.Name}] failed to build pipeline: {error?.Message}");
            }

            _logger.LogInformation($"[{_options.Name}] is built successfully.");

            IPipeline pipeline = serviceProvider.GetRequiredService<IPipeline>();

            return pipeline;
        }
        private Type ResolveServiceType(string serviceName, string messageName)
        {
            Type serviceType = ReflectionUtilities.GetTypeByNameOrFail(serviceName);

            if (!serviceType.IsGenericType)
            {
                return serviceType;
            }

            if (string.IsNullOrWhiteSpace(messageName))
            {
                throw new InvalidOperationException($"Type parameter for generic service \"{serviceName}\" is not provided.");
            }

            Type messageType = ReflectionUtilities.GetTypeByNameOrFail(messageName);

            try
            {
                serviceType = serviceType.MakeGenericType(messageType);
            }
            catch
            {
                throw;
            }

            return serviceType;
        }
        private Type GetSourceMessageType(Type sourceType)
        {
            if (sourceType.IsGenericType)
            {
                return sourceType.GetGenericArguments()[0];
            }

            Type? baseType = sourceType.BaseType;

            if (baseType == null || baseType.GetGenericTypeDefinition() != typeof(Source<>))
            {
                throw new InvalidOperationException($"Pipeline source type does not inherit from DaJet.Flow.Source<T> abstract class.");
            }

            return baseType.GetGenericArguments()[0];
        }
        private List<object> CreatePipelineServices(Type sourceInterfaceType, PipelineServiceProvider serviceProvider)
        {
            object service = serviceProvider.GetRequiredService(sourceInterfaceType);
            
            if (service is IConfigurable configurable)
            {
                configurable.Configure(_options.Source.Options);
            }

            List<object> services = new()
            {
                service
            };

            //Type serviceType = ResolveServiceType(_options!.Source.Type, _options.Source.Message);
            //object service = CreateServiceInstance(serviceProvider, serviceType, _options.Source.Options);
            //services.Add(service);

            Type serviceType;

            foreach (HandlerOptions handler in _options.Handlers)
            {
                serviceType = ResolveServiceType(handler.Type, null!);
                service = CreateServiceInstance(serviceProvider, serviceType, handler.Options);
                services.Add(service);
            }

            serviceType = ResolveServiceType(_options.Target.Type, _options.Target.Message);
            service = CreateServiceInstance(serviceProvider, serviceType, _options.Target.Options);
            services.Add(service);

            return services;
        }
        
        private object CreateServiceInstance(IServiceProvider serviceProvider, Type serviceType, Dictionary<string, string> options)
        {
            object? service = null;

            try
            {
                service = ActivatorUtilities.CreateInstance(serviceProvider, serviceType);

                if (service is IConfigurable configurable)
                {
                    configurable.Configure(options);
                }
            }
            catch
            {
                throw;
            }

            if (service == null)
            {
                throw new InvalidOperationException($"Failed to create service of type {serviceType}.");
            }

            return service;
        }
        private object AssemblePipeline(in List<object> services)
        {
            object current = services[0];

            for (int i = 1; i < services.Count; i++)
            {
                object service = services[i];

                Type linker = current.GetType();

                MethodInfo? linkTo = linker.GetMethod("LinkTo"); // ILinker<T>

                if (linkTo == null)
                {
                    throw new InvalidOperationException($"Method \"LinkTo\" is not found on type {linker}.");
                }

                try
                {
                    linkTo.Invoke(current, new object[] { service });
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException($"Failed to link {linker} to {service.GetType()}: {error.Message}.");
                }

                current = service;
            }
            
            return services[0];
        }
    }

    public sealed class TestDisposable : IDisposable
    {
        private readonly ILogger<TestDisposable> _logger;
        private object? resource = new object();
        public TestDisposable(PipelineServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetService<ILogger<TestDisposable>>();
        }
        public void Dispose()
        {
            resource = null;
            _logger.LogInformation("TestDisposable is disposed.");
        }
    }
}