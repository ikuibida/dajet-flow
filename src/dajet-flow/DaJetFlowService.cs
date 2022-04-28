using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaJet.Flow
{
    public sealed class DaJetFlowService : BackgroundService
    {
        private readonly IPipeline? _pipeline;
        private readonly ILogger<DaJetFlowService>? _logger;

        private CancellationToken _cancellationToken;
        public DaJetFlowService(IPipeline pipeline, ILogger<DaJetFlowService> logger)
        {
            _logger = logger;
            _pipeline = pipeline; // FIXME: may be null if failed to build
        }
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_pipeline == null)
            {
                return Task.CompletedTask;
            }

            _cancellationToken = cancellationToken;

            return Task.Factory.StartNew(DoWork, TaskCreationOptions.LongRunning);
        }
        private void DoWork()
        {
            if (_pipeline == null)
            {
                return;
            }

            string pipelineName = _pipeline.Options?.Name ?? string.Empty;
            int pipelineIdleTime = (_pipeline.Options == null ? 60 : _pipeline.Options.IdleTime);

            _logger?.LogInformation($"[{pipelineName}] is running ...");

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _pipeline.Run();

                    _logger?.LogInformation($"[{pipelineName}] is idle for {pipelineIdleTime} seconds ...");

                    Task.Delay(TimeSpan.FromSeconds(pipelineIdleTime)).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // do nothing - the wait task has been canceled
                }
                catch (Exception error)
                {
                    _logger?.LogError($"[{pipelineName}] exception:{Environment.NewLine}{error?.Message}", string.Empty);
                }
            }

            try
            {
                _pipeline.Dispose();
            }
            catch
            {
                // do nothing
            }

            _logger?.LogInformation($"[{pipelineName}] is disposed.");
        }
    }
}