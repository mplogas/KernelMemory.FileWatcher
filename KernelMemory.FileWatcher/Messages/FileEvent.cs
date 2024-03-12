using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
