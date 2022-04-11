namespace DaJet.Flow
{
    public interface ILinker<T>
    {
        void LinkTo(IProcessor<T> next);
    }
}