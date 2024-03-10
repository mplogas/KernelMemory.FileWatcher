using KernelMemory.FileWatcher.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using KernelMemory.FileWatcher.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace KernelMemory.FileWatcher.Services
{
    internal class FileMessageHandlerService : IRequestHandler<FileMessage>
    {
        private readonly ILogger<FileMessageHandlerService> logger;
        private readonly FileWatcherOptions options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ConcurrentQueue<FileMessage> eventQueue = new();

        private class QueueItem
        {
            public FileMessage Message { get; set; }
            public string Index { get; set; }
            public DateTime Time { get; } = DateTime.UtcNow;
        }

        public FileMessageHandlerService(ILogger<FileMessageHandlerService> logger, IOptions<FileWatcherOptions> options, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.options = options.Value;
            this.httpClientFactory = httpClientFactory;
        }

        public Task Handle(FileMessage message, CancellationToken cancellationToken)
        {
            //switch (message.MessageType)
            //{
            //    case MessageType.Create:
            //        this.logger.LogInformation($"File {message.FileName} created");
            //        break;
            //    case MessageType.Update:
            //        this.logger.LogInformation($"File {message.FileName} updated");
            //        break;
            //    case MessageType.Delete:
            //        this.logger.LogInformation($"File {message.FileName} deleted");
            //        break;
            //}

            var item = new QueueItem
            {
                Message = message,
                Index = options.Directories.Where(d => message.Directory.StartsWith(d.Path)).Select(d => d.Index).FirstOrDefault() ?? "default"
            };

            this.logger.LogInformation($"File: {message.FileName}, Type: {message.MessageType}, Index: {item.Index}");

            eventQueue.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
