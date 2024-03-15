namespace KernelMemory.FileWatcher.Configuration
{
    internal class FileWatcherDirectoryOptions
    {
        public string Path { get; set; }
        public string Filter { get; set; } = string.Empty;
        public List<string> Filters { get; set; } = new();
        public bool IncludeSubdirectories { get; set; }
        public string Index { get; set; }
        public bool InitialScan { get; set; }
    }
}
