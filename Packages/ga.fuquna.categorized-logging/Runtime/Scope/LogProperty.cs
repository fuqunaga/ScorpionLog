using System;

namespace CategorizedLogging
{
    public readonly struct LogProperty : IEquatable<LogProperty>
    {
        public string Key { get; }
        public string Value { get; }

        public LogProperty(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public void Deconstruct(out string key, out string value)
        {
            key = Key;
            value = Value;
        }

        public static implicit operator LogProperty((string key, string value) tuple)
        {
            return new LogProperty(tuple.key, tuple.value);
        }

        
        #region Equality
        
        public bool Equals(LogProperty other)
        {
            return Key == other.Key && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is LogProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value);
        }
        
        #endregion
    }
}