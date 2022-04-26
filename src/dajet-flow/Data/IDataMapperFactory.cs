using DaJet.Metadata;
using DaJet.Metadata.Model;

namespace DaJet.Flow.Data
{
    public interface IDataMapperFactory
    {
        IDataMapper<TMessage> CreateDataMapper<TMessage>(DataMapperOptions options) where TMessage : class, IMessage, new();
    }
    public abstract class DataMapperFactory : IDataMapperFactory
    {
        protected IServiceProvider ServiceProvider { get; }
        protected IMetadataService MetadataService { get; }
        public DataMapperFactory(IServiceProvider serviceProvider, IMetadataService metadataService)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            MetadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        }
        protected abstract IDataMapper<TMessage> GetDataMapper<TMessage>() where TMessage : class, IMessage, new();
        public virtual IDataMapper<TMessage> CreateDataMapper<TMessage>(DataMapperOptions options) where TMessage : class, IMessage, new()
        {
            IDataMapper<TMessage>? mapper = GetDataMapper<TMessage>();

            if (mapper == null)
            {
                throw new InvalidOperationException($"Data mapper for {typeof(TMessage)} is not found.");
            }

            if (!MetadataService
                .UseConnectionString(options.ConnectionString)
                .TryOpenInfoBase(out InfoBase infoBase, out string error))
            {
                throw new InvalidOperationException(error);
            }

            options.YearOffset = infoBase.YearOffset;

            ApplicationObject queue = infoBase.GetApplicationObjectByName(options.QueueObject);

            if (queue == null)
            {
                throw new InvalidOperationException($"Queue object [{options.QueueObject}] is not found.");
            }

            options.TableName = queue.TableName;
            options.SequenceName = queue.TableName + "_so";

            foreach (MetadataProperty property in queue.Properties)
            {
                if (property.Fields != null && property.Fields.Count == 1)
                {
                    DatabaseField field = property.Fields[0];

                    options.TableColumns.Add(property.Name, field.Name);
                }
            }

            // Configure data mapper

            mapper.Configure(options);

            return mapper;
        }
    }
}