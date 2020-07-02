using CTS.Callisto;
using CTS.Juno.Common;
using CTS.Oberon;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
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
                            .ConfigureLogging((logBuilder) =>
                            {
                                logBuilder.ClearProviders();
                                logBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                                logBuilder.AddLog4Net("log4net.config");
                            }).UseConsoleLifetime()
                            .ConfigureAppConfiguration((context, confiApp) =>
                            {
                                // NOTE: define process level environment variable if we don't want to bother
                                // with accessing the Azure Key vault:
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
                                    // here we use pre-defined app identity to get the secrets from the Azure Key vault
                                    // Note: Must run Visual Studio with Admin privileges to access these env variables

                                    Console.WriteLine("Attempting to get access to the Azure Key vault!");

                                    var appClientId = Environment.GetEnvironmentVariable("OceanlabAppIdentityClientId");
                                    var appClientSecret = Environment.GetEnvironmentVariable("OceanlabAppIdentityClientSecret");

                                    if (appClientId == null || appClientSecret == null)
                                    {
                                        throw new Exception("Unable to Access Azure Key Vault!");
                                    }

                                    var kvc = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(async (string authority, string resource, string scope) =>
                                    {
                                        var authContext = new AuthenticationContext(authority);
                                        var credential = new ClientCredential(appClientId, appClientSecret);
                                        AuthenticationResult result = await authContext.AcquireTokenAsync(resource, credential);

                                        if (result == null)
                                        {
                                            throw new InvalidOperationException("Failed to retrieve key Vault access token");
                                        }
                                        return result.AccessToken;
                                    }));

                                    confiApp.AddAzureKeyVault("https://kalypso.vault.azure.net/", kvc, new DefaultKeyVaultSecretManager());
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
