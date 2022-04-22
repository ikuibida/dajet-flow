using Microsoft.Extensions.DependencyInjection;

namespace DaJet.Flow
{
    public sealed class DatabaseConsumerBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        public DatabaseConsumerBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public object Build(SourceOptions options, out List<string> errors)
        {
            Type serviceType = GetDatabaseConsumerType(options, out errors);

            if (serviceType == null)
            {
                return null!;
            }

            object? consumer = null;

            try
            {
                consumer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType);

                if (consumer is IConfigurable configurable)
                {
                    configurable.Configure(options.Options);
                }
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create database consumer: [{options.Type}] {error.Message}");
            }

            return consumer!;
        }
        private Type GetConsumerType(SourceOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? genericType = null;

            if (string.IsNullOrWhiteSpace(options.Consumer))
            {
                if (options.Type == "SqlServer")
                {
                    genericType = ReflectionUtilities.GetTypeByName("DaJet.SqlServer.Consumer`1");
                }
                else if (options.Type == "PostgreSQL")
                {
                    genericType = ReflectionUtilities.GetTypeByName("DaJet.PostgreSQL.Consumer`1");
                }
                else
                {
                    errors.Add($"Default database consumer is not found: [{options.Type}]");
                }
            }
            else
            {
                genericType = ReflectionUtilities.GetTypeByName(options.Consumer);
            }

            if (genericType == null)
            {
                errors.Add($"Database consumer is not found: [{options.Type}] {options.Consumer}");
            }

            return genericType!;
        }
        private Type GetOutgoingMessageType(SourceOptions options, out List<string> errors)
        {
            errors = new List<string>();

            Type? messageType = null;

            if (string.IsNullOrWhiteSpace(options.Message))
            {
                messageType = ReflectionUtilities.GetTypeByName("DaJet.Flow.Contracts.OutgoingMessage");
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
        private Type GetDatabaseConsumerType(SourceOptions options, out List<string> errors)
        {
            Type? genericType = GetConsumerType(options, out errors);

            if (genericType == null)
            {
                return null!;
            }

            Type? messageType = GetOutgoingMessageType(options, out errors);

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
                errors.Add($"Failed to create database consumer type {options.Consumer}<{options.Message}>: {error.Message}");
            }

            if (serviceType == null && errors.Count == 0)
            {
                errors.Add($"Database consumer type {options.Consumer}<{options.Message}> is not found.");
            }

            return serviceType!;
        }
    }
}