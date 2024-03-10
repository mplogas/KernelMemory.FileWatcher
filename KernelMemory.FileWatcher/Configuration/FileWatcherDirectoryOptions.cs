namespace KernelMemory.FileWatcher.Configuration
{
    internal class FileWatcherDirectoryOptions
    {
        public string Path { get; set; }
        public string Filter { get; set; }
        public bool IncludeSubdirectories { get; set; }
        public string Index { get; set; }
    }
}
