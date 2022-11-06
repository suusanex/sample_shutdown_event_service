using Microsoft.Extensions.DependencyInjection;

namespace sample_shutdown_event_service_worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)

        {
            try
            {
                for (int i = 0; i < 12; i++)
                {
                    _logger.LogInformation($"Stop Wait Count={i}");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }


            }
            catch (Exception e)
            {
                _logger.LogWarning($"Stop Wait Exception={e}");
                throw;
            }
            await base.StopAsync(cancellationToken);

        }
    }
}