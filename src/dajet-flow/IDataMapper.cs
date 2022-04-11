using System.Data.Common;

namespace DaJet.Flow
{
    public interface IDataMapper<TMessage> where TMessage : class, IMessage, new()
    {
        void ConfigureSelect(in DbCommand command);
        void MapDataToMessage(in DbDataReader reader, in TMessage message);
        void ConfigureInsert(in DbCommand command, in TMessage message);
    }
}