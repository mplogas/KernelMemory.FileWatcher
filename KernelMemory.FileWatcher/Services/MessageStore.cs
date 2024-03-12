using System.Collections.Concurrent;
using System.Text;
using KernelMemory.FileWatcher.Configuration;
using KernelMemory.FileWatcher.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KernelMemory.FileWatcher.Services
{
    internal interface IMessageStore
    {
        Task Add(FileEvent fileEvent);
        public Message Take(Func<Message, bool> predicate);
        public bool Peek(Func<Message, bool> predicate);
    }

    internal class MessageStore : IMessageStore
    {
        private readonly ConcurrentBag<Message> bag = new();
        private readonly ILogger<MessageStore> logger;
        private readonly FileWatcherOptions options;

        public MessageStore(ILogger<MessageStore> logger, IOptions<FileWatcherOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
        }

        public Task Add(FileEvent fileEvent)
        {
            const char separator = '-';
            var option = options.Directories.FirstOrDefault(d => fileEvent.Directory.StartsWith(d.Path));

            if (option != null && fileEvent.Directory.StartsWith(option.Path))
            {
                var s = fileEvent.Directory[option.Path.Length..].Split(Path.DirectorySeparatorChar);
                var sb = new StringBuilder(option.Index);
                foreach (var subfolder in s)
                {
                    sb.Append(separator);
                    sb.Append(subfolder);
                }
                sb.Append(separator);
                sb.Append(fileEvent.FileName);

                var item = new Message
                {
                    Event = fileEvent,
                    Index = option.Index,
                    DocumentId = sb.ToString()
                };

                bag.Add(item);
                logger.LogInformation($"Added event for file {item.Event.FileName} of type {item.Event.EventType} to the store");
            }
            else
            {
                logger.LogWarning($"No matching directory found for file {fileEvent.FileName}");
            }
            return Task.CompletedTask;
        }

        public Message Take(Func<Message, bool> predicate)
        {
            try
            {
                //todo: requires a lock, i think maybe a blocking collection would be better vOv
                var messages = bag.Where(predicate)?.ToList();
                if (messages != null && messages.Any())
                {
                    var mostRecentItem = messages.OrderByDescending(msg => msg.Event.Time).First();
                    foreach (var msgToRemove in messages)
                    {
                        //can't use msgToRemove directly in TryTake, as it's a foreach variable
                        var msgCopy = msgToRemove;
                        if (!bag.TryTake(out msgCopy))
                        {
                            logger.LogWarning($"Failed to remove file {msgCopy?.Event.FileName ?? "n/a"} from the store");
                        }
                        logger.LogInformation($"Removed file {msgCopy?.Event.FileName ?? "n/a"} from the store");
                    }
                    return mostRecentItem;
                }
                else
                {
                    logger.LogWarning("No items matching the predicate were found.");
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error taking item from queue");
            }
            
            return null;
        }

        public bool Peek(Func<Message, bool> predicate)
        {
            return bag.FirstOrDefault(predicate) != null;
        }
    }
}

