namespace KernelMemory.FileWatcher.Services
{
    internal interface IFileWatcherFactory
    {
        public FileSystemWatcher Create(string directory, string filter, bool recursive);
    }

    internal class FileWatcherFactory : IFileWatcherFactory
    {
        public FileSystemWatcher Create(string directory, string filter, bool recursive = true)
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
            watcher.Filter = filter;
            return watcher;
        }
    }
}
