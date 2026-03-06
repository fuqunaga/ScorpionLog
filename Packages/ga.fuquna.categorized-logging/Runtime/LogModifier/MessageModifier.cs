using System;
using UnityEngine;

namespace CategorizedLogging
{
    public class MessageModifier : ILogModifier
    {
        private Func<string, string> ModifyMessageFunc { get; }

        
        public MessageModifier(Func<string, string> modifyMessageFunc)
        {
            ModifyMessageFunc = modifyMessageFunc;
        }
        
        [HideInCallstack]
        public LogEntry Modify(in LogEntry logEntry)
        {
            var modifiedMessage = ModifyMessageFunc?.Invoke(logEntry.Message) ?? logEntry.Message;
            return new LogEntry(logEntry.LogLevel, logEntry.Category, modifiedMessage);
        }
    }
    
    
    public static class LoggerExtensionsMessageModifier
    {
        public static ILogger ModifyMessage(this ILogger logger, Func<string, string> modifyMessageFunc)
        {
            return logger.AddModifier(new MessageModifier(modifyMessageFunc));
        }
    }
}