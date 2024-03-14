using System.Collections.Concurrent;
using System.Text;
using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace KernelMemory.FileWatcher.Services
{
    internal interface IMessageStore
    {
        Task Add(FileEvent fileEvent);
        public Message? TakeNext();
        public bool HasNext();
    }

    internal class MessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<string, Message> store = new();
        private readonly ILogger<MessageStore> logger;
        private readonly FileWatcherOptions options;
        private readonly ObjectPool<StringBuilder> pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

        public MessageStore(ILogger<MessageStore> logger, IOptions<FileWatcherOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
        }

        public Task Add(FileEvent fileEvent)
        {
            ArgumentNullException.ThrowIfNull(fileEvent);
            if (fileEvent.EventType == FileEventType.Ignore)
            {
                return Task.CompletedTask;
            }

            var option = options.Directories.FirstOrDefault(d => fileEvent.Directory.StartsWith(d.Path));
            if (option != null && fileEvent.Directory.StartsWith(option.Path))
            {
                var documentId = BuildDocumentId(option.Index, fileEvent.FileName);

                var item = new Message
                {
                    Event = fileEvent,
                    Index = option.Index,
                    DocumentId = documentId
                };

                store.AddOrUpdate(item.DocumentId, item, (key, message) => item);
                logger.LogInformation($"Added event {documentId} for file {item.Event.FileName} of type {item.Event.EventType} to the store");
            }
            else
            {
                logger.LogWarning($"No matching directory found for file {fileEvent.FileName}");
            }
            return Task.CompletedTask;
        }

        public Message? TakeNext()
        {
            return store.TryRemove(store.Keys.First(), out var message) ? message : null;
        }

        public bool HasNext()
        {
            return store.Count > 0;
        }

        private string BuildDocumentId(string index, string fileName)
        {
            const char separator = '_';

            var sb = pool.Get();
            sb.Append(index);
            sb.Append(separator);
            sb.Append(fileName.Replace(Path.DirectorySeparatorChar, separator).Replace(' ', separator));
            var result = sb.ToString();
            pool.Return(sb);

            return result;
        }
    }
}
