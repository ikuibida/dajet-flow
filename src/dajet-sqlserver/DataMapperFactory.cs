using DaJet.Flow.Contracts;
using DaJet.Flow.Data;
using DaJet.Metadata;
using DaJet.SqlServer.DataMappers;
using Microsoft.Extensions.DependencyInjection;

namespace DaJet.SqlServer
{
    public sealed class DataMapperFactory : DaJet.Flow.Data.DataMapperFactory
    {
        public DataMapperFactory(IServiceProvider serviceProvider, IMetadataService metadataService)
            : base(serviceProvider, metadataService)
        {
            MetadataService.UseDatabaseProvider(DatabaseProvider.SQLServer);
        }
        protected override IDataMapper<TMessage> GetDataMapper<TMessage>()
        {
            IDataMapper<TMessage>? mapper = null;

            if (typeof(TMessage) == typeof(OutgoingMessage))
            {
                mapper = ServiceProvider.GetService<OutgoingMessageDataMapper>() as IDataMapper<TMessage>;
            }
            else if (typeof(TMessage) == typeof(IncomingMessage))
            {
                mapper = ServiceProvider.GetService<IncomingMessageDataMapper>() as IDataMapper<TMessage>;
            }

            return mapper!;
        }
    }
}