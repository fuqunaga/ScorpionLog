using UnityEngine;

namespace CategorizedLogging
{
    public class UnityLoggerSinkConfig : SinkConfigMonoBehaviour<UnityLoggerSink>
    {
        [Header("LogType per LogLevel Settings")]
        public UnityLogTypeWithNone traceLogType = UnityLogTypeWithNone.Log;	
        public UnityLogTypeWithNone debugLogType = UnityLogTypeWithNone.Log;	
        public UnityLogTypeWithNone informationLogType = UnityLogTypeWithNone.Log;
        public UnityLogTypeWithNone warningLogType = UnityLogTypeWithNone.Warning;	
        public UnityLogTypeWithNone errorLogType = UnityLogTypeWithNone.Error;
        public UnityLogTypeWithNone fatalLogType = UnityLogTypeWithNone.Error;
        
        protected override void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            
            base.OnValidate();
            
            Sink.LogLevelToUnityLogTypeTable[LogLevel.Trace] = traceLogType.ToLogType();
            Sink.LogLevelToUnityLogTypeTable[LogLevel.Debug] = debugLogType.ToLogType();
            Sink.LogLevelToUnityLogTypeTable[LogLevel.Information] = informationLogType.ToLogType();
            Sink.LogLevelToUnityLogTypeTable[LogLevel.Warning] = warningLogType.ToLogType();
            Sink.LogLevelToUnityLogTypeTable[LogLevel.Error] = errorLogType.ToLogType();
            Sink.LogLevelToUnityLogTypeTable[LogLevel.Fatal] = fatalLogType.ToLogType();
        }
    }
}