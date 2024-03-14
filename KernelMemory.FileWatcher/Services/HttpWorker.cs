using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KernelMemory.FileWatcher.Services
{
    internal class HttpWorker : IHostedService, IDisposable
    {
        private readonly ILogger<HttpWorker> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMessageStore store;
        private readonly KernelMemoryOptions options;
        private PeriodicTimer? timer = null;

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
                while (store.HasNext())
                {
                    var message = store.TakeNext();
                    if (message != null && message.Event?.EventType != FileEventType.Ignore)
                    {
                        var endpoint = string.Empty;
                        var content = new MultipartFormDataContent();
                        var client = httpClientFactory.CreateClient("km-client");
                        if (message.Event.EventType == FileEventType.Upsert)
                        {
                            endpoint = "/upload";
                            var fileContent = new StreamContent(File.OpenRead(message.Event.Directory));
                            content.Add(fileContent, "file", message.Event.FileName);
                        }
                        else if (message.Event.EventType == FileEventType.Delete)
                        {
                            endpoint = "/delete";
                        }
                        content.Add(new StringContent(message.Index), "index");
                        content.Add(new StringContent(message.DocumentId), "documentid");

                        var response = await client.PostAsync(endpoint, content, cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            logger.LogInformation($"Posted message {message.DocumentId} to {options.Endpoint}");
                        }
                        else
                        {
                            logger.LogError($"Failed to post message {message.DocumentId} to {options.Endpoint}");
                        }
                    }
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
