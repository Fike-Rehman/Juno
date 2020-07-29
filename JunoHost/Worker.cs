using CTS.Common.Utilities;
using CTS.Callisto;
using CTS.Oberon;
using CTS.Juno.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace JunoHost
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly IRobin _robin;

        private readonly OberonEngine _oberonEngine;

        private readonly CallistoEngine _callistoEngine;

        private readonly ConcurrentBag<Task> _taskEngines = new ConcurrentBag<Task>();

        public Worker(ILogger<Worker> logger,
                      IRobin robin,
                      OberonEngine oberonEngine,
                      CallistoEngine callistoEngine)
        {
            _logger = logger;

            _robin = robin;

           _oberonEngine = oberonEngine;

           _callistoEngine = callistoEngine;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
              
                _logger.LogInformation($"Juno service running at {DateTime.Now}");
                await Task.Delay(5 * 60 * 1000, stoppingToken); // five minutes
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            if (WindowsServiceHelpers.IsWindowsService())
            {
                _logger.LogInformation("Starting Juno Gateway Service from Service Control Manager...");
            }
            else
            {
                _logger.LogInformation("Starting Juno Service. Please stand by...");
            }
                
            _robin.SpeakAsync("Starting Juno Service... Please stand by").Wait();

            // Start the Oberon Engine
            var oberonTask = Task.Run(() => _oberonEngine.Run(cancellationToken));

            _taskEngines.Add(oberonTask);

            _logger.LogInformation($"Oberon engine was started at {DateTimeOffset.Now}");

            cancellationToken.WaitHandle.WaitOne(500);

            // Start the Callisto Engine
            var callistoTask = Task.Run(() => _callistoEngine.Run(cancellationToken));

            _taskEngines.Add(callistoTask);

            _logger.LogInformation($"Callisto engine was started at {DateTimeOffset.Now}");

            return base.StartAsync(cancellationToken);
        }

        

        public override Task StopAsync(CancellationToken cancellationToken)
        {
             _robin.SpeakAsync("Stopping Juno Service. Please stand by...").Wait();

            _logger.LogInformation("Juno Service Stop requested!");

            try
            {
                Task.WaitAll(_taskEngines.ToArray(), TimeSpan.FromSeconds(25));

                int n = 3;
                while (n > 0)
                {
                    _logger.LogInformation($"Stopping Juno Service in {n} seconds...");
                    n--;
                    cancellationToken.WaitHandle.WaitOne(1000);
                }
            }
            catch (AggregateException ae)
            {
                // each InnerException should be OperationCanceledException, due to .Cancel() 
                foreach (var ex in ae.InnerExceptions)
                {
                    if (!(ex is OperationCanceledException))
                    {
                        _logger.LogError("Problem stopping task.", ex);
                    }
                }
            }

            _logger.LogInformation($"Juno Service Stopped at {DateTime.Now}");

            return base.StopAsync(cancellationToken);
        }
    }
}
