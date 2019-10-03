using CTS.Common.Utilities;
using CTS.Oberon;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JunoHost
{
    public class Worker : BackgroundService, IDisposable
    {
        private readonly ILogger<Worker> _logger;

        private readonly Robin _robin;

        private readonly IDeviceEngine _oberonEngine;

        public Worker(ILogger<Worker> logger, IDeviceEngine engine)
        {
            _logger = logger;

            _robin = new Robin();

            _oberonEngine = engine;    
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start the Oberon Engine
            var t =  Task.Run(() => _oberonEngine.Run(stoppingToken));

            _logger.LogInformation($"Oberon engine was started at {DateTimeOffset.Now}");

            await Task.Delay(1000, stoppingToken);

            // Any other engines can be started here:       
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
           
            _logger.LogInformation("Starting Juno Service. Please stand by...");

            _robin.SpeakAsync("Starting Juno Service... Please stand by").Wait();

            return base.StartAsync(cancellationToken);
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
