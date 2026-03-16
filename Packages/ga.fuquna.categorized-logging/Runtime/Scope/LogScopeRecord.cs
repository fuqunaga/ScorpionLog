using System;
using System.Collections.Generic;
using System.Threading;

namespace CategorizedLogging.Scope
{
    public record LogScopeRecord
    {
        #region Static members
        
        private static int _lastId;
        private static readonly AsyncLocal<LogScopeRecord> CurrentScope = new();
        private static readonly LogScopeRecord RootScope = new("Root");

        public static LogScopeRecord Current
        {
            get => CurrentScope.Value　?? RootScope;
            private set => CurrentScope.Value = value;
        }
        
        private static int GetNextId()
        {
            return Interlocked.Increment(ref _lastId);
        }
        
        #endregion
        
        
        private Dictionary<string, string> _properties;
        
        
        public int Id { get; } = GetNextId();
        public string Name { get; }
        public LogScopeRecord Parent { get; }
        public DateTime StartTimeUtc { get; } = DateTime.UtcNow;
        public DateTime EndTimeUtc { get; private set; }
        public DateTime StartTime => StartTimeUtc.ToLocalTime();
        public DateTime EndTime => EndTimeUtc.ToLocalTime();
        public IReadOnlyDictionary<string, string> Properties => _properties;

        
        public LogScopeRecord(string name = "", LogScopeRecord parent = null)
        {
            Name = name;
            Parent = parent ?? Current;
            Current = this;
        }

        public void SetProperty(string propertyName, string propertyValue)
        {
            if ( EndTimeUtc != default)
            {
                throw new InvalidOperationException("Cannot set property on a completed scope.");
            }
            
            _properties ??= new Dictionary<string, string>();
            _properties[propertyName] = propertyValue;
        }
        
        public void Complete()
        {
            if (EndTimeUtc != default)
            {
                throw new InvalidOperationException("Scope is already completed.");
            }
            
            EndTimeUtc = DateTime.UtcNow;
            
            if (Parent != null)
            {
                if (Current != this)
                {
                    // Scopeは本来親子関係にないものが並列に存在してもよいが現状実装がめんどくさくて例外扱い
                    // あまり無いと思うが需要が出たら対応したい
                    throw new InvalidOperationException("Current scope does not match the scope being completed.");
                }
                
                Current = Parent;
            }
        }
    }
}