namespace KernelMemory.FileWatcher.Services
{
    internal interface IFileWatcherFactory
    {
        public FileSystemWatcher Create(string directory, string filter, bool recursive);
        public FileSystemWatcher Create(string directory, List<string> filters, bool recursive);
    }

    internal class FileWatcherFactory : IFileWatcherFactory
    {
        public FileSystemWatcher Create(string directory, string filter, bool recursive = true)
        {
            var watcher = BuildFileSystemWatcher(directory, recursive);
            watcher.Filter = filter;

            return watcher;
        }

        public FileSystemWatcher Create(string directory, List<string> filters, bool recursive)
        {
            var watcher = BuildFileSystemWatcher(directory, recursive);
            foreach (var filter in filters)
            {
                watcher.Filters.Add(filter);
            }
            return watcher;
        }

        private FileSystemWatcher BuildFileSystemWatcher(string directory, bool recursive = true)
        {
            //BUG: a deleted subfolder is not catched by FileSystemWatcher (including all containing files)
            var watcher = new FileSystemWatcher(directory);
            watcher.NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Size;
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = recursive;

            return watcher;
        }
    }
}
