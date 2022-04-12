using DaJet.Metadata;
using DaJet.Metadata.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DaJet.Flow
{
    public sealed class PipelineBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PipelineBuilder> _logger;
        public PipelineBuilder(IServiceProvider serviceProvider, ILogger<PipelineBuilder> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public IPipeline Build(PipelineOptions options)
        {
            object consumer = null;
            if (options.Source.Type == "SqlServer" ||
                options.Source.Type == "PostgreSQL")
            {
                consumer = CreateDatabaseConsumer(in options);
            }
            else if (options.Source.Type == "RabbitMQ")
            {
                Type serviceType = GetTypeByName(options.Source.Consumer);
                consumer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, options.Source.Options);
            }

            object producer = null;
            if (options.Target.Type == "SqlServer" ||
                options.Target.Type == "PostgreSQL")
            {
                producer = CreateDatabaseProducer(in options);
            }
            else if (options.Target.Type == "RabbitMQ")
            {
                Type serviceType = GetTypeByName(options.Target.Producer);
                producer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, options.Target.Options);
            }

            // 3.0 Create handlers
            List<object> handlers = new List<object>();
            foreach (string handlerName in options.Handlers)
            {
                Type serviceType = GetTypeByName(handlerName);
                object handler = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType);
                handlers.Add(handler);
            }
            handlers.Add(producer);

            // 4.0 Assemble pipeline

            MethodInfo linkTo;
            object current = consumer;
            foreach (object handler in handlers)
            {
                linkTo = current.GetType().GetMethod("LinkTo");
                linkTo.Invoke(current, new object[] { handler });
                current = handler;
            }

            // 5.0 Create pipeline
            string pipelineName = options.Name;
            Type inputMessageType = GetTypeByName(options.Source.Message);
            Type pipelineType = typeof(Pipeline<>).MakeGenericType(inputMessageType);
            object pipeline = ActivatorUtilities.CreateInstance(_serviceProvider, pipelineType, pipelineName, consumer);

            return pipeline as IPipeline;
        }
        private static Type GetTypeByName(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                //TODO: load assemblies which are not referenced or not loaded yet

                Type type = assembly.GetType(name);

                if (type is not null)
                {
                    return type;
                }
            }

            return null;
        }

        private object CreateDatabaseConsumer(in PipelineOptions options)
        {
            // 1.0 Create consumer data mapper
            Type mapperType = GetTypeByName(options.Source.DataMapper);
            DatabaseOptions databaseOptions = CreateDatabaseOptions(options.Source.Type, options.Source.Options);
            object mapper = ActivatorUtilities.CreateInstance(_serviceProvider, mapperType, databaseOptions);

            // 1.1 Create consumer
            Type genericType = GetTypeByName(options.Source.Consumer);
            Type messageType = GetTypeByName(options.Source.Message);
            Type serviceType = genericType.MakeGenericType(messageType);
            object consumer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, databaseOptions, mapper);

            return consumer;
        }
        private object CreateDatabaseProducer(in PipelineOptions options)
        {
            // 2.0 Create producer data mapper
            Type mapperType = GetTypeByName(options.Target.DataMapper);
            DatabaseOptions databaseOptions = CreateDatabaseOptions(options.Target.Type, options.Target.Options);
            object mapper = ActivatorUtilities.CreateInstance(_serviceProvider, mapperType, databaseOptions);

            // 2.1 Create producer
            Type genericType = GetTypeByName(options.Target.Producer);
            Type messageType = GetTypeByName(options.Target.Message);
            Type serviceType = genericType.MakeGenericType(messageType);
            object producer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, databaseOptions, mapper);

            return producer;
        }
        private static DatabaseOptions CreateDatabaseOptions(string provider, Dictionary<string, string> settings)
        {
            if (!settings.TryGetValue(nameof(DatabaseOptions.ConnectionString), out string connectionString))
            {
                throw new ArgumentException(nameof(DatabaseOptions.ConnectionString));
            }

            if (!settings.TryGetValue(nameof(DatabaseOptions.QueueObject), out string queueObject))
            {
                throw new ArgumentException(nameof(DatabaseOptions.QueueObject));
            }

            if (!settings.TryGetValue(nameof(DatabaseOptions.MessagesPerTransaction), out string messagesPerTransaction))
            {
                messagesPerTransaction = "0";
            }

            DatabaseProvider databaseProvider = (provider == "SqlServer"
                ? DatabaseProvider.SQLServer
                : DatabaseProvider.PostgreSQL);

            if (!new MetadataService()
                .UseConnectionString(connectionString)
                .UseDatabaseProvider(databaseProvider)
                .TryOpenInfoBase(out InfoBase infoBase, out string error))
            {
                Console.WriteLine(error);
                throw new InvalidOperationException(error);
            }

            ApplicationObject queue = infoBase.GetApplicationObjectByName(queueObject);

            DatabaseOptions options = new()
            {
                ConnectionString = connectionString,
                DatabaseProvider = provider,
                YearOffset = infoBase.YearOffset,
                QueueTable = queue.TableName,
                SequenceObject = queue.TableName + "_so",
                MessagesPerTransaction = int.Parse(messagesPerTransaction)
            };

            foreach (MetadataProperty property in queue.Properties)
            {
                DatabaseField field = property.Fields[0];

                if (property.Name == "НомерСообщения" ||
                    property.Name == "МоментВремени" ||
                    property.Name == "Идентификатор")
                {
                    options.OrderColumns.Add(property.Name, field.Name);
                }

                options.TableColumns.Add(property.Name, field.Name);
            }

            return options;
        }
    }
}