namespace DaJet.Flow
{
    public interface IPipeline
    {
        void Execute();
        //void Suspend();
        //void Continue();
        //void Close();
        string Name { get; }
    }
    public sealed class Pipeline<TSource> : IPipeline
    {
        private readonly ISource<TSource> _source;
        private readonly CancellationTokenSource _token = new();
        public Pipeline(string name, ISource<TSource> source)
        {
            Name = name;
            _source = source;
        }
        public string Name { get; private set; }
        public void Execute()
        {
            _source.Pump(_token.Token);
        }
    }
}