using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using KernelMemory.FileWatcher.Services;
using MediatR;
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

            await StartWatcher(host.Services);

            await host.RunAsync();
        }

        private static async Task StartWatcher(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var fileWatcher = provider.GetRequiredService<IFileWatcherService>();
            await fileWatcher.Watch();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var basePath = AppContext.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", false)
                .AddUserSecrets<Program>()
                .Build();

            FileWatcherOptions options = new();
            configuration.GetSection("FileWatcher").Bind(options);

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
                    services.AddLogging(c => c.AddSerilog().AddConsole());
                    services.AddHttpClient();
                    services.AddMediatR(cfg =>
                    {
                        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                    });
                    services.AddSingleton<IFileWatcherFactory, FileWatcherFactory>();
                    services.AddScoped<IFileWatcherService, FileWatcherService>();
                    services.AddScoped<IRequestHandler<FileMessage>, FileMessageHandlerService>();
                })
                .UseConsoleLifetime();
        }
    }
}
