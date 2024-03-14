using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KernelMemory.FileWatcher.Messages
{
    internal enum FileEventType
    {
        Upsert,
        Delete,
        Ignore
    }
}
