using CTS.Callisto;
using CTS.Juno.Common;
using CTS.Oberon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

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

        private static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                            .UseWindowsService()
                            .ConfigureLogging((context, logger) =>
                            {
                                logger.AddLog4Net().SetMinimumLevel(LogLevel.Debug);
                            })
                            .ConfigureAppConfiguration((context, confiApp) =>
                            {
                                var env = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

                                confiApp.SetBasePath(Directory.GetCurrentDirectory());
                                confiApp.AddJsonFile("appsettings.json", optional: true);
                                confiApp.AddEnvironmentVariables();
                                confiApp.AddCommandLine(args);

                                if(env == "Development")
                                {
                                    confiApp.AddUserSecrets(typeof(Program).Assembly);
                                }
                                else
                                {
                                    confiApp.AddAzureKeyVault("https://kalypso.vault.azure.net/");
                                }
                            })
                            .ConfigureServices((context, services) =>
                            {
                                services.AddOptions();
                                services.Configure<AppSettings>(context.Configuration);
                               
                                services.AddHostedService<Worker>()
                                        .AddSingleton<IAppSettings, AppSettings>()
                                        .AddSingleton<ISecureSettings, SecureSettings>()
                                        .AddSingleton<IJunoDevice, JunoDevice>()
                                        .AddSingleton(typeof(OberonEngine))
                                        .AddSingleton(typeof(CallistoEngine));          
                            });
    }
}
