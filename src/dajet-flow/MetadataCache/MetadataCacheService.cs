using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaJet.Flow
{
    internal sealed class MetadataCacheService : BackgroundService
    {
        private readonly IMetadataCache _cache;
        private readonly ILogger<MetadataCacheService> _logger;
        private CancellationToken _cancellationToken;
        public MetadataCacheService(IMetadataCache cache, ILogger<MetadataCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            return Task.Factory.StartNew(DoWork, TaskCreationOptions.LongRunning);
        }
        private void DoWork()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TryDoWork();

                    Task.Delay(TimeSpan.FromSeconds(_cache.Options.RefreshTimeout)).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // do nothing - the wait task has been canceled
                }
                catch (Exception error)
                {
                   _logger.LogError(error, $"[{nameof(MetadataCacheService)}] Error.");
                }
            }

            _logger.LogInformation($"[{nameof(MetadataCacheService)}] Shutdown.");
        }
        private void TryDoWork()
        {
            _logger.LogInformation($"[{nameof(MetadataCacheService)}] Updating metadata cache ...");

            _cache.Refresh(out string error); // Initialize or refresh metadata cache

            if (string.IsNullOrEmpty(error))
            {
                _logger.LogInformation($"[{nameof(MetadataCacheService)}] Metadata cache updated successfully.");
            }
            else
            {
                _logger.LogError(error, $"[{nameof(MetadataCacheService)}] Failed to update metadata cache.");
            }
        }
    }
}