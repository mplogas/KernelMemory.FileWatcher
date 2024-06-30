using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;

namespace KernelMemory.FileWatcher
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            StartWatcher(host.Services);

            await host.RunAsync();
        }

        private static void StartWatcher(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var fileWatcher = provider.GetRequiredService<IFileWatcherService>();
            fileWatcher.Watch();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            
            var basePath = File.Exists("/config/appsettings.json") ? "/config" : AppContext.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", false)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices(services =>
                {
                    services.AddSerilog((services, lc) => lc
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File(Path.Exists("/config/logs") ? "/config/logs/km-filewatcher.log" : "logs/km-filewatcher.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                        .MinimumLevel.Warning()
                        .ReadFrom.Configuration(configuration));
                    services.Configure<FileWatcherOptions>(configuration.GetSection("FileWatcher"));
                    services.Configure<KernelMemoryOptions>(configuration.GetSection("KernelMemory"));
                    services.AddSingleton<IMessageStore, MessageStore>();
                    services.AddHttpClient("km-client", client =>
                    {
                        client.BaseAddress = new Uri(configuration.GetValue<string>("KernelMemory:Endpoint") ??
                                                     "http://localhost:9001/");
                        var apiKey = configuration.GetValue<string>("KernelMemory:ApiKey") ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(apiKey))
                        {
                            client.DefaultRequestHeaders.Add("Authorization", apiKey);
                        }
                    }).AddPolicyHandler(GetRetryPolicy(configuration.GetValue<int>("KernelMemory:Retries", 2)));
                    services.AddSingleton<IFileWatcherFactory, FileWatcherFactory>();
                    services.AddScoped<IFileWatcherService, FileWatcherService>();
                    services.AddHostedService<HttpWorker>();
                })
                .UseConsoleLifetime();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: retries, fastFirst: true);

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                //.WaitAndRetryAsync(retries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
                .WaitAndRetryAsync(delay);
        }
    }
}
