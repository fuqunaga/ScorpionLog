using JetBrains.Annotations;

namespace CategorizedLogging
{
    /// <summary>
    /// スコープとはLogPropertyを保持する単位
    /// /// </summary>
    public static partial class Log
    {
        [MustDisposeResource]
        public static LogScope BeginScope(in LogProperty property)
        {
            return new LogScope(property);
        }

        [MustDisposeResource]
        public static LogScope BeginScope(string propertyName, string propertyValue)
            => BeginScope((propertyName, propertyValue));
        
        [MustDisposeResource]
        public static LogScope BeginScope<T>(string propertyName, in T propertyValue)
            => BeginScope(propertyName, propertyValue.ToString());

    }
}