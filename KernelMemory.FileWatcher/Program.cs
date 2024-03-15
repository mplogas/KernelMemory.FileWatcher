using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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
            var basePath = AppContext.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", false)
                .AddUserSecrets<Program>()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices(services =>
                {
                    services.Configure<FileWatcherOptions>(configuration.GetSection("FileWatcher"));
                    services.Configure<KernelMemoryOptions>(configuration.GetSection("KernelMemory"));
                    services.AddLogging(c => c.AddSerilog().AddConsole());
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
                    });
                    services.AddSingleton<IFileWatcherFactory, FileWatcherFactory>();
                    services.AddScoped<IFileWatcherService, FileWatcherService>();
                    services.AddHostedService<HttpWorker>();
                })
                .UseConsoleLifetime();
        }
    }
}
