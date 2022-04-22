using Microsoft.Extensions.DependencyInjection;

namespace DaJet.Flow
{
    public sealed class DatabaseProducerBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        public DatabaseProducerBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public object Build(TargetOptions options, out List<string> errors)
        {
            Type serviceType = GetDatabaseProducerType(options, out errors);

            if (serviceType == null)
            {
                return null!;
            }

            object? producer = null;

            try
            {
                producer = ActivatorUtilities.CreateInstance(_serviceProvider, serviceType);

                if (producer is IConfigurable configurable)
                {
                    configurable.Configure(options.Options);
                }
            }
            catch (Exception error)
            {
                errors.Add($"Failed to create database producer: [{options.Type}] {error.Message}");
            }

            return producer!;
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