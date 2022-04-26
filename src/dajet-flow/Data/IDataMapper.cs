using System.Data.Common;

namespace DaJet.Flow.Data
{
    public interface IDataMapper<TMessage> where TMessage : class, IMessage, new()
    {
        void Configure(DataMapperOptions options);
        void ConfigureSelect(in DbCommand command);
        void MapDataToMessage(in DbDataReader reader, in TMessage message);
        void ConfigureInsert(in DbCommand command, in TMessage message);

        //TODO: !?
        //void ConfigureUpdate(in DbCommand command, in TMessage message);
        //void ConfigureDelete(in DbCommand command, in TMessage message);
    }
}