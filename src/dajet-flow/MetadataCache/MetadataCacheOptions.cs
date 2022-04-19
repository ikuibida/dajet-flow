using DaJet.Metadata;

namespace DaJet.Flow
{
    public sealed class MetadataCacheOptions
    {
        public int RefreshTimeout { get; set; } = 600; // seconds
        public string ConnectionString { get; set; } = string.Empty;
        public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.SQLServer;
    }
}