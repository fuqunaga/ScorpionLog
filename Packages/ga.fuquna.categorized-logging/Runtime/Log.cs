using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global
namespace CategorizedLogging
{
    /// <summary>
    /// CategorizedLogging インターフェース
    /// 
    /// カテゴリとして呼び出し元の型を使用する
    /// </summary>
    public static partial class Log
    {
        private static readonly AsyncLocal<ILogDispatcher> LogDispatcherAsyncLocal = new();
        
        
        public static ILogDispatcher LogDispatcher { get; set; } = new LogDispatcher();
        
        public static ILogDispatcher AsyncLocalLogDispatcher
        {
            get => LogDispatcherAsyncLocal.Value;
            set => LogDispatcherAsyncLocal.Value = value;
        }


        [HideInCallstack]
        private static void EmitLog(LogRecord logRecord)
        {
            LogDispatcher?.Log(logRecord);
            AsyncLocalLogDispatcher?.Log(logRecord);
        }

  
        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EmitLog(LogLevel logLevel, string message, 
            [CallerFilePath] string callerFilePath = "", 
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
            )
        {
            EmitLog(new LogRecord(logLevel, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }


        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Trace(string message,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            EmitLog(new LogRecord(LogLevel.Trace, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Debug(string message,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            EmitLog(new LogRecord(LogLevel.Debug, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }
        
        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Information(string message,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            EmitLog(new LogRecord(LogLevel.Information, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string message,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            EmitLog(new LogRecord(LogLevel.Warning, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string message,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            EmitLog(new LogRecord(LogLevel.Error, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Critical(string message,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            EmitLog(new LogRecord(LogLevel.Critical, message,
                new CallerInformation(callerFilePath, callerLineNumber, callerMemberName)));
        }
    }
}