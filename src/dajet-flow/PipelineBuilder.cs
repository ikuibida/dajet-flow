using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            Type sourceType = ReflectionUtilities.GetTypeByNameOrFail(_options!.Source.Type);

            Type messageType = GetSourceMessageType(sourceType);
            
            IPipeline pipeline = CreatePipeline(messageType);
            
            pipeline.Configure(_options);

            try
            {
                List<object> services = CreatePipelineServices(pipeline.Services);
                
                object source = AssemblePipeline(in services);

                pipeline.ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(Source<>).MakeGenericType(messageType), source);
                });
            }
            catch (Exception error)
            {
                _logger.LogError($"Failed to build pipeline [{_options.Name}]: {error.Message}");
            }

            _logger.LogInformation($"Pipeline [{pipeline!.Options.Name}] is built successfully.");

            return pipeline!;
        }
        private IPipeline CreatePipeline(Type messageType)
        {
            Type pipelineType = typeof(Pipeline<>).MakeGenericType(messageType);

            object? pipeline = ActivatorUtilities.CreateInstance(_serviceProvider, pipelineType);

            if (pipeline == null)
            {
                throw new InvalidOperationException($"Failed to create pipeline [{_options!.Name}] instance.");
            }

            return (pipeline as IPipeline)!;
        }
        private Type GetSourceMessageType(Type sourceType)
        {
            if (sourceType.IsGenericType)
            {
                return ReflectionUtilities.GetTypeByNameOrFail(_options!.Source.Message);
            }

            Type? baseType = sourceType.BaseType;

            if (baseType == null || baseType.GetGenericTypeDefinition() != typeof(Source<>))
            {
                throw new InvalidOperationException($"Pipeline source type does not inherit from DaJet.Flow.Source<T> abstract class.");
            }

            return baseType.GetGenericArguments()[0];
        }
        private List<object> CreatePipelineServices(IServiceProvider serviceProvider)
        {
            List<object> services = new();

            Type serviceType = ResolveServiceType(_options!.Source.Type, _options.Source.Message);
            object service = CreateServiceInstance(serviceProvider, serviceType, _options.Source.Options);
            services.Add(service);

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
}