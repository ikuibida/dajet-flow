using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DaJet.Flow.App
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
            _cancellationToken = cancellationToken;
            return Task.Factory.StartNew(DoWork, TaskCreationOptions.LongRunning);
        }
        private void DoWork()
        {
            _logger.LogInformation($"Pipeline [{_pipeline.Name}] is running ...");

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TryDoWork();

                    //_logger.LogInformation($"{_pipeline.Name} sleep 30 seconds ...");

                    Task.Delay(TimeSpan.FromSeconds(1)).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // do nothing - the wait task has been canceled
                }
                catch (Exception error)
                {
                    _logger.LogTrace($"{_pipeline.Name}{Environment.NewLine}{error}", string.Empty);
                }
            }

            _logger.LogInformation($"Pipeline [{_pipeline.Name}] is stopped.");
        }
        private void TryDoWork()
        {
            Stopwatch watch = new();
            watch.Start();
            _pipeline.Execute();
            watch.Stop();
            //_logger.LogWarning($"{_pipeline.Name} elapsed in {watch.ElapsedMilliseconds} ms");
        }
    }
}