using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using Serilog.Data;

namespace KernelMemory.FileWatcher.Services
{
    internal class HttpWorker : IHostedService, IDisposable
    {
        private readonly ILogger<HttpWorker> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMessageStore store;
        private readonly KernelMemoryOptions options;
        private PeriodicTimer? timer;

        public HttpWorker(ILogger<HttpWorker> logger, IOptions<KernelMemoryOptions> options, IHttpClientFactory httpClientFactory, IMessageStore messageStore)
        {
            this.logger = logger;
            this.options = options.Value;
            this.httpClientFactory = httpClientFactory;
            this.store = messageStore;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting HttpWorker");
            timer = new PeriodicTimer(options.Schedule);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var messages = store.TakeAll();
                if (messages.Any())
                {
                    var tasks = new List<Action>();

                    foreach (var message in messages)
                    {
                        tasks.Add(async () =>
                        {
                            if (message.Event?.EventType != FileEventType.Ignore)
                            {
                                logger.LogInformation($"Processing message {message.DocumentId} for file {message.Event?.FileName} of type {message.Event?.EventType}");
                                var client = httpClientFactory.CreateClient("km-client");
                                string endpoint;
                                HttpResponseMessage? response = null;

                                if (message.Event is { EventType: FileEventType.Upsert })
                                {
                                    endpoint = "/upload";

                                    var content = new MultipartFormDataContent();
                                    var fileContent = new StreamContent(File.OpenRead(message.Event.Directory));
                                    content.Add(fileContent, "file", message.Event.FileName);
                                    content.Add(new StringContent(message.Index), "index");
                                    content.Add(new StringContent(message.DocumentId), "documentid");
                                    response = await client.PostAsync(endpoint, content);
                                }
                                else if (message.Event is { EventType: FileEventType.Delete })
                                {
                                    endpoint = $"/documents?index={message.Index}&documentId={message.DocumentId}";
                                    response = await client.DeleteAsync(endpoint);
                                }

                                if (response is { IsSuccessStatusCode: true })
                                {
                                    logger.LogInformation($"Sent message {message.DocumentId} to {options.Endpoint}");
                                }
                                else
                                {
                                    logger.LogError($"Failed to send message {message.DocumentId} to {options.Endpoint}");
                                }
                            }
                        });
                    }

                    var parallelOptions = new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = options.ParallelUploads
                    };
                    Parallel.Invoke(parallelOptions, tasks.ToArray());

                }
                else
                {
                    logger.LogInformation("Nothing to process");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
