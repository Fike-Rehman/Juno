using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using CTS.Common.Utilities;

namespace JunoHost
{
    public class Worker : BackgroundService, IDisposable
    {
        private readonly ILogger<Worker> _logger;

        private Robin robin;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            robin = new Robin();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
           
            _logger.LogInformation("Starting Juno Service. Please stand by...");

            robin.SpeakAsync("Starting Juno Service... Please stand by").Wait();

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Juno Service Stop requested!");
            robin.SpeakAsync("Stoping Juno Service. Please stand by...").Wait();

            int n = 3;
            while (n > 0)
            {
                _logger.LogInformation($"Stoping Juno Service in {n} seconds...");
                n--;
                Task.Delay(1000, cancellationToken);
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
