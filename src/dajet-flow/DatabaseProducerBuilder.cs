using DaJet.Metadata;
using DaJet.Metadata.Model;
using Microsoft.Extensions.DependencyInjection;

namespace DaJet.Flow
{
    internal sealed class DatabaseProducerBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMetadataService _metadataService;
        internal DatabaseProducerBuilder(IServiceProvider serviceProvider, IMetadataService metadataService)
        {
            _serviceProvider = serviceProvider;
            _metadataService = metadataService;
        }
        internal object Build(TargetOptions options, out List<string> errors)
        {
            object mapper = CreateDataMapper(options, out errors);

            if (mapper == null)
            {
                return null!;
            }

            Type serviceType = GetDatabaseProducerType(options, out errors);

            if (serviceType == null)
            {
                return null!;
            }

            object? producer = null;

            try
            {
                producer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType, _serviceProvider, options.Options, mapper);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create database producer: [{options.Type}] {error.Message}");
            }

            return producer!;
        }

        private DataMapperOptions CreateDataMapperOptions(Dictionary<string, string> options, out List<string> errors)
        {
            errors = new List<string>();

            DataMapperOptions mapperOptions = new DataMapperOptions();

            if (options.TryGetValue(nameof(DataMapperOptions.MessagesPerTransaction), out string? messagesPerTransaction)
                && !string.IsNullOrWhiteSpace(messagesPerTransaction)
                && int.TryParse(messagesPerTransaction, out int parameterValue))
            {
                mapperOptions.MessagesPerTransaction = (parameterValue > 0 ? parameterValue : 1000);
            }

            if (!options.TryGetValue(nameof(DataMapperOptions.QueueObject), out string? queueObject)
                || string.IsNullOrWhiteSpace(queueObject))
            {
                return mapperOptions;
            }

            if (!_metadataService.TryOpenInfoBase(out InfoBase infoBase, out string error))
            {
                errors.Add(error);
                
                return null!;
            }

            mapperOptions.YearOffset = infoBase.YearOffset;

            ApplicationObject queue = infoBase.GetApplicationObjectByName(queueObject);

            if (queue == null)
            {
                errors.Add($"Queue object [{queueObject}] is not found.");
                
                return null!;
            }

            mapperOptions.TableName = queue.TableName;
            mapperOptions.SequenceName = queue.TableName + "_so";

            foreach (MetadataProperty property in queue.Properties)
            {
                if (property.Fields != null && property.Fields.Count == 1)
                {
                    DatabaseField field = property.Fields[0];

                    mapperOptions.TableColumns.Add(property.Name, field.Name);
                }
            }

            return mapperOptions;
        }

        private Type GetDataMapperType(TargetOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? mapperType = null;

            if (string.IsNullOrWhiteSpace(options.DataMapper))
            {
                if (options.Type == "SqlServer")
                {
                    mapperType = ReflectionUtilities.GetTypeByName("DaJet.SqlServer.DataMappers.IncomingMessageDataMapper");
                }
                else if (options.Type == "PostgreSQL")
                {
                    mapperType = ReflectionUtilities.GetTypeByName("DaJet.PostgreSQL.DataMappers.IncomingMessageDataMapper");
                }
                else
                {
                    errors.Add($"Default data mapper is not found: [{options.Type}]");
                }
            }
            else
            {
                mapperType = ReflectionUtilities.GetTypeByName(options.DataMapper);
            }

            if (mapperType == null)
            {
                errors.Add($"Data mapper is not found: [{options.Type}] {options.DataMapper}");
            }

            return mapperType!;
        }
        private object CreateDataMapper(TargetOptions options, out List<string> errors)
        {
            DataMapperOptions? mapperOptions = CreateDataMapperOptions(options.Options, out errors);

            if (mapperOptions == null)
            {
                return null!;
            }

            Type? mapperType = GetDataMapperType(options, out errors);

            if (mapperType == null)
            {
                return null!;
            }

            object? mapper = null;

            try
            {
                mapper = ActivatorUtilities.CreateInstance(_serviceProvider, mapperType, mapperOptions);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create data mapper: [{options.Type}] {error.Message}");
            }

            return mapper!;
        }

        private Type GetProducerType(TargetOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? genericType = null;

            if (string.IsNullOrWhiteSpace(options.Producer))
            {
                if (options.Type == "SqlServer")
                {
                    genericType = ReflectionUtilities.GetTypeByName("DaJet.SqlServer.Producer`1");
                }
                else if (options.Type == "PostgreSQL")
                {
                    genericType = ReflectionUtilities.GetTypeByName("DaJet.PostgreSQL.Producer`1");
                }
                else
                {
                    errors.Add($"Default database producer is not found: [{options.Type}]");
                }
            }
            else
            {
                genericType = ReflectionUtilities.GetTypeByName(options.Producer);
            }

            if (genericType == null)
            {
                errors.Add($"Database producer is not found: [{options.Type}] {options.Producer}");
            }

            return genericType!;
        }
        private Type GetIncomingMessageType(TargetOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? messageType = null;

            if (string.IsNullOrWhiteSpace(options.Message))
            {
                messageType = ReflectionUtilities.GetTypeByName("DaJet.Flow.Contracts.IncomingMessage");
            }
            else
            {
                messageType = ReflectionUtilities.GetTypeByName(options.Message);
            }

            if (messageType == null)
            {
                errors.Add($"Message type is not found: {options.Message}");
            }

            return messageType!;
        }
        private Type GetDatabaseProducerType(TargetOptions options, out List<string> errors)
        {
            Type? genericType = GetProducerType(options, out errors);

            if (genericType == null)
            {
                return null!;
            }

            Type? messageType = GetIncomingMessageType(options, out errors);

            if (messageType == null)
            {
                return null!;
            }

            errors = new List<string>();

            Type? serviceType = null;

            try
            {
                serviceType = genericType.MakeGenericType(messageType);
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create database producer type {options.Producer}<{options.Message}>: {error.Message}");
            }

            if (serviceType == null && errors.Count == 0)
            {
                errors.Add($"Database producer type {options.Producer}<{options.Message}> is not found.");
            }

            return serviceType!;
        }
    }
}