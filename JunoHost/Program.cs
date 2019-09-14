using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CTS.Oberon;

namespace JunoHost
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello World!");
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
                                        .AddSingleton(typeof(IDeviceEngine), typeof(OberonEngine));
                            });
    }
}
