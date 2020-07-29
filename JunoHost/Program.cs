using CTS.Callisto;
using CTS.Common.Utilities;
using CTS.Juno.Common;
using CTS.Oberon;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Diagnostics;
using System.IO;

namespace JunoHost
{
    public class Program
    {
       
        public static void Main(string[] args)
        {
            // Create a logger manually so we can log during Host creation
            var builtConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddCommandLine(args)
            .Build();

            const string loggerTemplate = @"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}]<{ThreadId}> [{SourceContext:l}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(builtConfig["LogFilePath"], LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Debug, loggerTemplate, theme: SystemConsoleTheme.Colored)
                .CreateLogger();

            try
            {
                Log.Information("++++ Welcome to Juno service ++++");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception x)
            {
                if (x.GetType() != typeof(OperationCanceledException))
                {
                    Log.Fatal(x, "There was a problem starting the Juno Service");
                }
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                    .UseWindowsService()

                    .ConfigureAppConfiguration((context, confiApp) =>
                    {
                        // if the app is starting from the Service Control Manager, we 
                        // must set the Current Directory so it is not pointing to "Windows/System..."
                        if (WindowsServiceHelpers.IsWindowsService())
                        {
                            var processModule = Process.GetCurrentProcess().MainModule;
                            if (processModule != null)
                            {
                                var pathToExe = processModule.FileName;
                                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                                Directory.SetCurrentDirectory(pathToContentRoot);
                            }
                        }

                        confiApp.SetBasePath(Directory.GetCurrentDirectory());

                        confiApp.AddJsonFile("appsettings.json", optional: false);
                        confiApp.AddEnvironmentVariables();
                        confiApp.AddCommandLine(args);

                        // NOTE: define process level environment variable if we don't want to bother
                        // with accessing the Azure Key vault:
                        var env = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

                        if (env == "Development")
                        {
                            // Console.WriteLine("Adding user secrets file...");
                            confiApp.AddUserSecrets(typeof(Program).Assembly);
                        }
                        else
                        {
                            //here we use pre-defined app identity to get the secrets from the Azure Key vault
                            //Note: Must run Visual Studio with Admin privileges to access these env variables

                            Log.Information("Attempting to read device keys from Azure Key vault!");

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
                        .AddSingleton(typeof(OberonEngine))   // NOTE: DI fails to resolve two dependencies of the same type 
                        .AddSingleton(typeof(CallistoEngine)) // so this is just a work around
                        .AddSingleton<IAppSettings, AppSettings>()
                        .AddSingleton<ISecureSettings, SecureSettings>()
                        .AddSingleton<IRobin, Robin>();
                        
                    })
                    .UseSerilog();  
        }
    }
}
