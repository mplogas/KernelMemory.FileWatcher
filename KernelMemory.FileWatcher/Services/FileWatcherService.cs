using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KernelMemory.FileWatcher.Services
{
    internal interface IFileWatcherService
    {
        void Watch();
    }

    internal class FileWatcherService : IFileWatcherService
    {
        private readonly ILogger<FileWatcherService> logger;
        private readonly IFileWatcherFactory fileWatcherFactory;
        private readonly FileWatcherOptions options;
        private readonly IMessageStore messageStore;

        public FileWatcherService(ILogger<FileWatcherService> logger,IFileWatcherFactory fileWatcherFactory,IMessageStore messageStore,IOptions<FileWatcherOptions> options)
        {
            this.logger = logger;
            this.fileWatcherFactory = fileWatcherFactory;
            this.messageStore = messageStore;
            this.options = options.Value;
        }

        public void Watch()
        {
            foreach (var directory in options.Directories)
            {
                var watcher = fileWatcherFactory.Create(directory.Path, directory.Filter, directory.IncludeSubdirectories);

                watcher.Changed += EnqueueEvent;
                watcher.Created += EnqueueEvent;
                watcher.Deleted += EnqueueEvent;
                watcher.Renamed += EnqueueEvent;
                watcher.Error += OnError;

                watcher.EnableRaisingEvents = true;
            }
        }

        private void EnqueueEvent(object sender, FileSystemEventArgs e)
        {
            //we need to throttle the events here, as filewatcher can raise multiple events (of the same type) for a single file change
            var eventType = ConvertEventTypes(e.ChangeType);
            if (!messageStore.Peek(i => i.Event.FileName==e.Name && i.Event.EventType.Equals(eventType)))
            {
                messageStore.Add(new FileEvent { EventType = eventType, FileName = e.Name ?? "n/a", Directory = e.FullPath });
            }
            else
            {
                logger.LogInformation($"event for file {e.Name} already in the queue");
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            logger.LogError(e.GetException(), "An error occurred in the file watcher.");
        }

        private static FileEventType ConvertEventTypes(WatcherChangeTypes wct)
        {
            switch (wct)
            {
                case WatcherChangeTypes.Deleted:
                    return FileEventType.Delete;
                case WatcherChangeTypes.Renamed:
                    return FileEventType.Rename;
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Created:
                    return FileEventType.Upsert;
                default:
                    return FileEventType.Ignore;
            }
        }
    }
}
