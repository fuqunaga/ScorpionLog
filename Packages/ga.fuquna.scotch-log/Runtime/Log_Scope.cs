using System.Runtime.CompilerServices;
using ScotchLog.Scope;

namespace ScotchLog
{
    /// <summary>
    /// スコープとはLogScopePropertyを保持する単位
    /// /// </summary>
    public static partial class Log
    {
        /// <summary>
        /// 同一スレッドにおけるスコープを開始します
        /// 主にusingとともに使用してDisposeされることを想定しています
        /// </summary>
        public static LogScope BeginScope(string name = "")
        {
            return new LogScope(name);
        }

        /// <summary>
        /// 同一スレッドにおけるスコープを開始します
        /// 主にusingとともに使用してDisposeされることを想定しています
        /// </summary>
        public static LogScope BeginPropertyScope<T>(
            T propertyValue,
            [CallerArgumentExpression("propertyValue")] string propertyName = "")
        {
            return new LogScope().SetProperty(propertyName, propertyValue);
        }
    }
}