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

        public FileWatcherService(ILogger<FileWatcherService> logger, IFileWatcherFactory fileWatcherFactory, IMessageStore messageStore, IOptions<FileWatcherOptions> options)
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
                var watcher = directory.Filters.Any() ? fileWatcherFactory.Create(directory.Path, directory.Filters, directory.IncludeSubdirectories) : fileWatcherFactory.Create(directory.Path, directory.Filter, directory.IncludeSubdirectories);

                watcher.Changed += EnqueueEvent;
                watcher.Created += EnqueueEvent;
                watcher.Deleted += EnqueueEvent;
                watcher.Renamed += EnqueueEvent;
                watcher.Error += OnError;

                watcher.EnableRaisingEvents = true;

                if (directory.InitialScan)
                {
                    Task.Run(() => InitialScan(directory));
                }
                logger.LogInformation($"Watching {directory.Path}");
            }
        }

        private Task InitialScan(FileWatcherDirectoryOptions directory)
        {
            var files = Directory.GetFiles(directory.Path, directory.Filter, directory.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                messageStore.Add(new FileEvent { EventType = FileEventType.Upsert, FileName = Path.GetFileName(file), Directory = file });
            }
            logger.LogInformation($"Initial scan completed for {directory.Path}");

            return Task.CompletedTask;
        }

        private void EnqueueEvent(object sender, FileSystemEventArgs e)
        {
            var eventType = ConvertEventTypes(e.ChangeType);
            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                var args = (RenamedEventArgs)e;
                messageStore.Add(new FileEvent { EventType = FileEventType.Delete, FileName = args.OldName ?? "n/a", Directory = args.OldFullPath }); //filename n/a is pretty useless but the compiler is ruthless with nullable warnings
                messageStore.Add(new FileEvent { EventType = FileEventType.Upsert, FileName = args.Name ?? "n/a", Directory = args.FullPath });
            }
            else
            {
                messageStore.Add(new FileEvent { EventType = eventType, FileName = e.Name ?? "n/a", Directory = e.FullPath });
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
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Created:
                    return FileEventType.Upsert;
                default:
                    return FileEventType.Ignore;
            }
        }
    }
}
