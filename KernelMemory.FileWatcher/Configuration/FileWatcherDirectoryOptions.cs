namespace KernelMemory.FileWatcher.Configuration
{
    internal class FileWatcherDirectoryOptions
    {
        public required string Path { get; set; }
        public string Filter { get; set; } = string.Empty;
        public bool IncludeSubdirectories { get; set; }
        public string Index { get; set; } = "default";
        public bool InitialScan { get; set; }
    }
}
