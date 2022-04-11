namespace DaJet.Flow
{
    public interface ISource<T> : ILinker<T>, IDisposable
    {
        void Pump(CancellationToken token);
    }
    public abstract class Source<T> : ISource<T>
    {
        private IProcessor<T>? _pipeline;
        public void LinkTo(IProcessor<T> processor)
        {
            _pipeline = processor;
        }
        public abstract void Pump(CancellationToken token);
        protected void _Process(in T output)
        {
            _pipeline?.Process(in output);
        }
        protected void _Synchronize()
        {
            _pipeline?.Synchronize();
        }
        public void Dispose()
        {
            _Dispose();
            _pipeline?.Dispose();
        }
        protected virtual void _Dispose()
        {
            // do nothing by default
        }
    }
}