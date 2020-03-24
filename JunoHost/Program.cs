using CTS.Callisto;
using CTS.Oberon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace JunoHost
{
    public static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("*** Welcome to Juno ***");
                CreateHostBuilder(args).Build().Run();
            }
            catch(Exception x)
            {
                Console.WriteLine($"Juno Service was stopped by the user; {x.Message}");
            }  
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                            .UseWindowsService()
                            .ConfigureLogging((context, logger) =>
                            {
                                logger.AddLog4Net().SetMinimumLevel(LogLevel.Debug);
                            })
                            .ConfigureServices((hostContext, services) =>
                            {
                                services.AddHostedService<Worker>()
                                        .AddSingleton(typeof(OberonEngine))
                                        .AddSingleton(typeof(CallistoEngine));
                            });
    }
}
