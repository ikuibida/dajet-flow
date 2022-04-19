using DaJet.Metadata;
using DaJet.Metadata.Model;
using Microsoft.Extensions.Options;

namespace DaJet.Flow
{
    public interface IMetadataCache : IDisposable
    {
        void Refresh(out string error);
        bool TryGet(out InfoBase infoBase);
        MetadataCacheOptions Options { get; }
    }
    public sealed class MetadataCache : IMetadataCache
    {
        private const string CACHE_KEY = "METADATA";
        private readonly RWLockSlim _lock = new RWLockSlim();
        private readonly Dictionary<string, InfoBase> _cache = new Dictionary<string, InfoBase>();
        private readonly IOptions<MetadataCacheOptions> _options;
        public MetadataCacheOptions Options { get { return _options.Value; } }
        public MetadataCache(IOptions<MetadataCacheOptions> options)
        {
            _options = options;
        }
        void IMetadataCache.Refresh(out string error)
        {
            using (_lock.WriteLock())
            {
                if (new MetadataService()
                    .UseDatabaseProvider(Options.DatabaseProvider)
                    .UseConnectionString(Options.ConnectionString)
                    .TryOpenInfoBase(out InfoBase infoBase, out error))
                {
                    if (_cache.ContainsKey(CACHE_KEY))
                    {
                        _cache[CACHE_KEY] = infoBase;
                    }
                    else
                    {
                        _cache.Add(CACHE_KEY, infoBase);
                    }
                }
            }
        }
        bool IMetadataCache.TryGet(out InfoBase infoBase)
        {
            using (_lock.ReadLock())
            {
                return _cache.TryGetValue(CACHE_KEY, out infoBase);
            }
        }
        void IDisposable.Dispose()
        {
            _cache.Clear();
        }
    }
}