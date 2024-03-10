using System.Collections.Concurrent;
using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KernelMemory.FileWatcher.Services
{
    internal interface IFileWatcherService
    {
        Task Watch();
    }

    internal class FileWatcherService : IFileWatcherService
    {
        private readonly ConcurrentQueue<FileSystemEventArgs> eventQueue = new();
        private readonly ILogger<FileWatcherService> logger;
        private readonly IFileWatcherFactory fileWatcherFactory;
        private readonly IMediator mediatr;
        private readonly FileWatcherOptions options;

        public FileWatcherService(ILogger<FileWatcherService> logger,IFileWatcherFactory fileWatcherFactory,IMediator mediatr,IOptions<FileWatcherOptions> options)
        {
            this.logger = logger;
            this.fileWatcherFactory = fileWatcherFactory;
            this.mediatr = mediatr;
            this.options = options.Value;
        }

        public async Task Watch()
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

            await ProcessEvents();
        }

        private void EnqueueEvent(object sender, FileSystemEventArgs e)
        {
            //we need to throttle the events here, as filewatcher can raise multiple events (of the same type) for a single file change
            if(!eventQueue.Any(i => i.Name==e.Name && i.ChangeType == e.ChangeType))
            {
                eventQueue.Enqueue(e);
            }
        }

        private async Task ProcessEvents()
        {
            while (true)
            {
                if (eventQueue.TryDequeue(out var fileEvent))
                {
                    await HandleEvent(fileEvent);
                }
                else
                {
                    // Sleep for a short duration to avoid busy waiting
                    await Task.Delay(100);
                }
            }
        }

        private async Task HandleEvent(FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    await mediatr.Send(new FileMessage { MessageType = MessageType.Update, FileName = e.Name, Directory = e.FullPath });
                    logger.LogInformation($"File {e.Name} updated");
                    break;
                case WatcherChangeTypes.Created:
                    await mediatr.Send(new FileMessage { MessageType = MessageType.Create, FileName = e.Name, Directory = e.FullPath });
                    logger.LogInformation($"File {e.Name} created");
                    break;
                case WatcherChangeTypes.Deleted:
                    await mediatr.Send(new FileMessage { MessageType = MessageType.Delete, FileName = e.Name, Directory = e.FullPath });
                    logger.LogInformation($"File {e.Name} deleted");
                    break;
                case WatcherChangeTypes.Renamed:
                    var renamedEvent = (RenamedEventArgs)e;
                    await mediatr.Send(new FileMessage { MessageType = MessageType.Delete, FileName = renamedEvent.OldName, Directory = renamedEvent.OldFullPath });
                    await mediatr.Send(new FileMessage { MessageType = MessageType.Create, FileName = renamedEvent.Name, Directory = renamedEvent.FullPath });
                    logger.LogInformation($"File {renamedEvent.OldName} renamed to {renamedEvent.Name}");
                    break;
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            logger.LogError(e.GetException(), "An error occurred in the file watcher.");
        }
    }
}
