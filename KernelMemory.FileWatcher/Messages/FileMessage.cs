using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KernelMemory.FileWatcher.Messages
{
    internal class FileMessage : IRequest
    {
        public MessageType MessageType { get; set; }
        public string FileName { get; set; }
        public string Directory { get; set; }
    }
}
