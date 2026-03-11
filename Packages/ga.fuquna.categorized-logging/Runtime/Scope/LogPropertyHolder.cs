using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CategorizedLogging
{
    public class LogPropertyHolder
    {
        private static int _nextContextId;
        
        
        private bool _isDirty = true;
        private string _cachedLogString = "";
        
        private readonly List<(int propertyId, LogProperty property)> _contextList = new();
        
        
        public IEnumerable<LogProperty> Contexts => _contextList.Select(pair => pair.property);
        public int ContextCount => _contextList.Count;
        public bool HasContext => ContextCount > 0;
        
        
        public int Add(in LogProperty logProperty)
        {
            var currentId = _nextContextId++;
            _contextList.Add((currentId, logProperty));
            _isDirty = true;
            return currentId;
        }

        public void Remove(int contextId)
        {
            var index = _contextList.FindIndex(pair => pair.propertyId == contextId);
            if (index < 0)
            {
                Log.Warning(this, $"Trying to remove log context with id {contextId}, but it does not exist.");
            }
            else
            {
                _contextList.RemoveAt(index);
                _isDirty = true;
            }
        }
        
        public string ToLogString()
        {
            if (ContextCount <= 0)
            {
                return "";
            }
            
            if (!_isDirty)
            {
                return _cachedLogString;
            }
            
            var sb = new StringBuilder();
            foreach (var context in Contexts)
            {
                sb.Append('[').Append(context.Key).Append(": ").Append(context.Value).Append(']');
            }
            return _cachedLogString = sb.ToString();
        }
    }
}