using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KernelMemory.FileWatcher.Services
{
    internal class HttpWorker : BackgroundService
    {
        private readonly ILogger<HttpWorker> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMessageStore store;
        private readonly KernelMemoryOptions options;

        public HttpWorker(ILogger<HttpWorker> logger, IOptions<KernelMemoryOptions> options, IHttpClientFactory httpClientFactory, IMessageStore messageStore)
        {
            this.logger = logger;
            this.options = options.Value;
            this.httpClientFactory = httpClientFactory;
            this.store = messageStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation("Starting HttpWorker");
            
            using PeriodicTimer timer = new PeriodicTimer(options.Schedule);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
                {
                    var messages = store.TakeAll();
                    if (messages.Any())
                    {
                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = stoppingToken,
                            MaxDegreeOfParallelism = options.ParallelUploads
                        };
                        Parallel.Invoke(parallelOptions, messages.Select(message => (Action)(() => BuildMessageTask(message, stoppingToken))).ToArray());
                    }
                    else
                    {
                        logger.LogInformation("Nothing to process");
                    
                    }
                }
            } catch (OperationCanceledException)
            {
                this.logger.LogInformation("Stopping HttpWorker");
            }
        }

        private async Task BuildMessageTask(Message message, CancellationToken stoppingToken)
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
                    response = await client.PostAsync(endpoint, content, stoppingToken);
                }
                else if (message.Event is { EventType: FileEventType.Delete })
                {
                    endpoint = $"/documents?index={message.Index}&documentId={message.DocumentId}";
                    response = await client.DeleteAsync(endpoint, stoppingToken);
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
        }
    }
}
