using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using CTS.Common.Utilities;
using CTS.Oberon;

namespace JunoHost
{
    public class Worker : BackgroundService, IDisposable
    {
        private readonly ILogger<Worker> _logger;

        private readonly Robin _robin;

        private readonly OberonEngine _oberonEngine;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            _robin = new Robin();

            _oberonEngine = new OberonEngine();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
           
            _logger.LogInformation("Starting Juno Service. Please stand by...");

            _robin.SpeakAsync("Starting Juno Service... Please stand by").Wait();

            var task = Task.Factory.StartNew(() => _oberonEngine.Run(cancellationToken));

            return task;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Juno Service Stop requested!");
            _robin.SpeakAsync("Stoping Juno Service. Please stand by...").Wait();

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
