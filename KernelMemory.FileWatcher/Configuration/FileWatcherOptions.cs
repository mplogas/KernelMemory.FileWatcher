namespace KernelMemory.FileWatcher.Configuration
{
    internal class FileWatcherOptions
    {
        public List<FileWatcherDirectoryOptions> Directories { get; set; }
        public int Cooldown { get; set; }
    }
}
