namespace KernelMemory.FileWatcher.Configuration
{
    internal class KernelMemoryOptions
    {
        public string Endpoint { get; set; } = "http://localhost:9001";
        public string ApiKey { get; set; } = string.Empty;
        public TimeSpan Schedule { get; set; }
        public int Retries { get; set; } = 2;
        public int ParallelUploads { get; set; } = 4;
    }
}
