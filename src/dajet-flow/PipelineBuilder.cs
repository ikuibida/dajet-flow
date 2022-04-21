using DaJet.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DaJet.Flow
{
    public sealed class PipelineBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        public PipelineBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IPipeline Build(PipelineOptions options, out List<string> errors)
        {
            object? consumer = CreateConsumerService(options.Source, out errors);

            if (consumer == null) { return null!; }
            
            object? producer = CreateProducerService(options.Target, out errors);

            if (producer == null) { return null!; }

            List<object>? handlers = CreateMessageHandlers(options, out errors);

            if (handlers == null) { return null!; }

            if (!AssemblePipeline(in consumer, in handlers, in producer, out errors))
            {
                return null!;
            }

            return CreatePipeline(options, in consumer, out errors);
        }

        private object CreateConsumerService(in SourceOptions options, out List<string> errors)
        {
            object? consumer = null;

            if (options.Type == "SqlServer" || options.Type == "PostgreSQL")
            {
                consumer = CreateDatabaseConsumer(options, out errors);
            }
            else if (options.Type == "RabbitMQ" || options.Type == "ApacheKafka")
            {
                consumer = CreateBrokerConsumer(options, out errors);
            }
            else
            {
                errors = new List<string>() { $"Unknown source type: {options.Type}" };
            }

            return consumer!;
        }
        private object CreateBrokerConsumer(in SourceOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? serviceType = null;

            if (string.IsNullOrWhiteSpace(options.Consumer))
            {
                if (options.Type == "RabbitMQ")
                {
                    serviceType = ReflectionUtilities.GetTypeByName("DaJet.RabbitMQ.Consumer");
                }
                else if (options.Type == "ApacheKafka")
                {
                    serviceType = ReflectionUtilities.GetTypeByName("DaJet.ApacheKafka.Consumer");
                }
                else
                {
                    errors.Add($"Default consumer is not found: [{options.Type}]");
                }
            }
            else
            {
                serviceType = ReflectionUtilities.GetTypeByName(options.Consumer);
            }

            if (serviceType == null)
            {
                if (errors.Count == 0)
                {
                    errors.Add($"Consumer type is not found: [{options.Type}] {options.Consumer}");
                }

                return null!;
            }

            object? consumer = null;

            try
            {
                consumer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, _serviceProvider, options.Options);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create consumer: [{options.Type}] {error.Message}");
            }

            return consumer!;
        }
        private object CreateDatabaseConsumer(in SourceOptions options, out List<string> errors)
        {
            errors = new List<string>();

            DatabaseProvider databaseProvider =
                (options.Type == "SqlServer"
                ? DatabaseProvider.SQLServer
                : DatabaseProvider.PostgreSQL);

            if (!options.Options.TryGetValue("ConnectionString", out string? connectionString)
                || string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add($"Database connection string is not defined.");
                
                return null!;
            }

            IMetadataService metadataService = new MetadataService()
                .UseDatabaseProvider(databaseProvider)
                .UseConnectionString(connectionString);

            DatabaseConsumerBuilder builder = new(_serviceProvider, metadataService);

            return builder.Build(options, out errors);
        }

        private object CreateProducerService(in TargetOptions options, out List<string> errors)
        {
            object? consumer = null;

            if (options.Type == "SqlServer" || options.Type == "PostgreSQL")
            {
                consumer = CreateDatabaseProducer(options, out errors);
            }
            else if (options.Type == "RabbitMQ" || options.Type == "ApacheKafka")
            {
                consumer = CreateBrokerProducer(options, out errors);
            }
            else
            {
                errors = new List<string>() { $"Unknown target type: {options.Type}" };
            }

            return consumer!;
        }
        private object CreateBrokerProducer(in TargetOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? serviceType = null;

            if (string.IsNullOrWhiteSpace(options.Producer))
            {
                if (options.Type == "RabbitMQ")
                {
                    serviceType = ReflectionUtilities.GetTypeByName("DaJet.RabbitMQ.Producer");
                }
                else if (options.Type == "ApacheKafka")
                {
                    serviceType = ReflectionUtilities.GetTypeByName("DaJet.ApacheKafka.Producer");
                }
                else
                {
                    errors.Add($"Default producer is not found: [{options.Type}]");
                }
            }
            else
            {
                serviceType = ReflectionUtilities.GetTypeByName(options.Producer);
            }

            if (serviceType == null)
            {
                if (errors.Count == 0)
                {
                    errors.Add($"Producer type is not found: [{options.Type}] {options.Producer}");
                }

                return null!;
            }

            object? producer = null;

            try
            {
                producer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, _serviceProvider, options.Options);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create producer: [{options.Type}] {error.Message}");
            }

            return producer!;
        }
        private object CreateDatabaseProducer(in TargetOptions options, out List<string> errors)
        {
            errors = new List<string>();

            DatabaseProvider databaseProvider =
                (options.Type == "SqlServer"
                ? DatabaseProvider.SQLServer
                : DatabaseProvider.PostgreSQL);

            if (!options.Options.TryGetValue("ConnectionString", out string? connectionString)
                || string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add($"Database connection string is not defined.");

                return null!;
            }

            IMetadataService metadataService = new MetadataService()
                .UseDatabaseProvider(databaseProvider)
                .UseConnectionString(connectionString);

            DatabaseProducerBuilder builder = new(_serviceProvider, metadataService);

            return builder.Build(options, out errors);
        }

        private List<object> CreateMessageHandlers(PipelineOptions options, out List<string> errors)
        {
            errors = new List<string>();

            List<object> handlers = new();

            foreach (string typeName in options.Handlers)
            {
                object? handler = CreateMessageHandler(typeName, in errors);

                if (handler == null)
                {
                    return null!;
                }

                handlers.Add(handler);
            }
            
            return handlers;
        }
        private object CreateMessageHandler(string name, in List<string> errors)
        {
            Type handlerType = ReflectionUtilities.GetTypeByName(name);

            if (handlerType == null)
            {
                errors.Add($"Handler type is not found: {name}");

                return null!;
            }

            object? handler = null;

            try
            {
                handler = ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create handler [{name}]: {error.Message}");
            }

            return handler!;
        }

        private bool AssemblePipeline(in object consumer, in List<object> handlers, in object producer, out List<string> errors)
        {
            errors = new List<string>();

            handlers.Add(producer);

            object current = consumer;

            foreach (object handler in handlers)
            {
                Type handlerType = current.GetType();

                MethodInfo? linkTo = handlerType.GetMethod("LinkTo");

                if (linkTo == null)
                {
                    errors.Add($"Method \"LinkTo\" is not found on type {handlerType}");

                    return false;
                }

                try
                {
                    linkTo.Invoke(current, new object[] { handler });
                }
                catch (Exception error)
                {
                    errors.Add($"Failed to link {current.GetType()} to {handlerType}: {error.Message}");

                    return false;
                }

                current = handler;
            }
            
            return true;
        }

        private IPipeline CreatePipeline(PipelineOptions options, in object consumer, out List<string> errors)
        {
            errors = new List<string>();

            Type consumerType = consumer.GetType();

            Type? baseType = consumerType.BaseType;

            if (baseType == null || baseType.GetGenericTypeDefinition() != typeof(Source<>))
            {
                errors.Add($"Consumer does not inherit from DaJet.Flow.Source<TMessage> abstract class.");

                return null!;
            }

            Type messageType = baseType.GetGenericArguments()[0];


            Type? pipelineType = null;

            try
            {
                pipelineType = typeof(Pipeline<>).MakeGenericType(messageType);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create type Pipeline<{messageType}>: {error.Message}");
            }

            if (pipelineType == null)
            {
                return null!;
            }

            object? pipeline = null;

            try
            {
                pipeline = ActivatorUtilities.CreateInstance(_serviceProvider, pipelineType, options.Name, consumer);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create pipeline [{options.Name}]: {error.Message}");
            }

            if (pipeline == null)
            {
                return null!;
            }

            return (pipeline as IPipeline)!;
        }
    }
}