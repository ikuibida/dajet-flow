namespace DaJet.Flow
{
    public interface IOptionsBuilder<TOptions> where TOptions : class, new()
    {
        TOptions Build(Dictionary<string, string> options);
    }
    public abstract class OptionsBuilder<TOptions> : IOptionsBuilder<TOptions> where TOptions : class, new()
    {
        public abstract TOptions Build(Dictionary<string, string> options);
    }
}