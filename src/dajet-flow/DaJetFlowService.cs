using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaJet.Flow
{
    public sealed class DaJetFlowService : BackgroundService
    {
        private readonly IPipeline _pipeline;
        private readonly ILogger<DaJetFlowService> _logger;

        private CancellationToken _cancellationToken;
        public DaJetFlowService(IPipeline pipeline, ILogger<DaJetFlowService> logger)
        {
            _pipeline = pipeline;
            _logger = logger;
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
            _logger.LogInformation($"Pipeline [{_pipeline.Options.Name}] is running ...");

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TryDoWork();

                    _logger.LogInformation($"[{_pipeline.Options.Name}] is idle for {_pipeline.Options.IdleTime} seconds ...");

                    Task.Delay(TimeSpan.FromSeconds(_pipeline.Options.IdleTime)).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // do nothing - the wait task has been canceled
                }
                catch (Exception error)
                {
                    _logger.LogError($"{_pipeline.Options.Name}{Environment.NewLine}{error}", string.Empty);
                }
            }

            _logger.LogInformation($"Pipeline [{_pipeline.Options.Name}] is stopped.");
        }
        private void TryDoWork()
        {
            _pipeline.Execute();
        }
    }
}