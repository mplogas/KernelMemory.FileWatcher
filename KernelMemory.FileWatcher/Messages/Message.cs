namespace KernelMemory.FileWatcher.Messages;

internal class Message
{
    public FileEvent? Event { get; set; }
    public string Index { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
}