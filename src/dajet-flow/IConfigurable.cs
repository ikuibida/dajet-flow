namespace DaJet.Flow
{
    public interface IConfigurable<TOptions> where TOptions : class, new()
    {
        void Configure(in TOptions options);
    }
}