namespace KernelMemory.FileWatcher.Messages
{
    internal class FileEvent
    {
        public FileEventType EventType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public DateTime Time { get; } = DateTime.UtcNow;

    }
}
