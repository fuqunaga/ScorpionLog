using CategorizedLogging.Scope;
using JetBrains.Annotations;

namespace CategorizedLogging
{
    /// <summary>
    /// スコープとはLogPropertyを保持する単位
    /// /// </summary>
    public static partial class Log
    {
        [MustDisposeResource]
        public static LogScope BeginScope(string name = "")
        {
            return new LogScope(name);
        }
    }
}