using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RosettaUI;
using Unity.Collections;
using UnityEngine;

namespace ScotchLog.Samples
{
    public class SampleUI : MonoBehaviour
    {
        [SerializeField]
        private RosettaUIRoot rosettaUIRoot;
        
        private void Start()
        {
            rosettaUIRoot.Build(CreateLogEmitWindow());
            rosettaUIRoot.Build(CreateMemorySinkWindow());
        }


        private Element CreateLogEmitWindow()
        {
            return UI.Window(UI.Column(
                    CreateLogEmitUI(),
                    UI.Space().SetHeight(20f),
                    CreateUnityLogEmitUI()
                )
            );
        }

        private Element CreateLogEmitUI()
        {
            var logMessage = "This is a sample log message.";
            var logLevel = LogLevel.Information;

            var logMessageField = UI.TextArea(() => logMessage);
            var logLevelField = UI.Field(() => logLevel);

            var emitButton = UI.Button("Emit Log", () =>
            {
                Log.EmitLog(logLevel, logMessage);
            });


            var threadCount = 10;
            
            var threadCountField =UI.Field(() => threadCount);
            var emitMultiThreadButton = UI.Button("Emit Log from Multiple Threads", () =>
            {
                for (var i = 0; i < threadCount; i++)
                {
                    var threadIndex = i;
                    Task.Run(() =>
                    {
                        Log.EmitLog(logLevel, $"[Thread {threadIndex}] {logMessage}");
                    });
                }
            });


            return　UI.Page(
                logMessageField,
                logLevelField,
                emitButton,
                UI.Space().SetHeight(10f),
                threadCountField,
                emitMultiThreadButton
            );

        }

        private static Element CreateUnityLogEmitUI()
        {
            var logMessage = "This is a sample Unity log message.";
            var logType = LogType.Warning;

            var logMessageField = UI.TextArea(() => logMessage);
            var logTypeField = UI.Field(() => logType);

            var emitButton = UI.Button("Debug.unityLogger.Log()", () =>
            {
                Debug.unityLogger.Log(logType, logMessage);
            });

            return　UI.Page(
                UI.Label($"<b>{nameof(UnityLogRedirector)}</b>"),
                UI.Indent(
                    UI.HelpBox($"Debug.Log()などのUnityのログ出力を{nameof(ScotchLog)}にリダイレクトします"),
                    UI.Toggle(nameof(UnityLogRedirector.Enabled), () => UnityLogRedirector.Enabled),
                    logMessageField,
                    logTypeField,
                    emitButton
                )
            );
        }

        
        private static Element CreateMemorySinkWindow()
        {
            var logText = string.Empty;
            
            var memorySinkConfig = FindAnyObjectByType<MemorySinkConfig>();
            var memorySink = memorySinkConfig.Sink;
            
            var mainThreadContext = SynchronizationContext.Current;
            memorySink.onLogEntryAddedMultiThreaded += () =>
            {
                if (mainThreadContext != null)
                {
                    mainThreadContext.Post(_ => UpdateLogField(), null);
                }
                else
                {
                    UpdateLogField();
                }
            };


            return UI.Window("Memory Sink Log Entries",
                    UI.ScrollViewVerticalAndHorizontal(null, null,
                        UI.Row(
                            UI.Space(),
                            UI.Button("Clear Log", () =>
                            {
                                memorySink.Clear();
                                UpdateLogField();
                            })
                        ),
                        UI.TextArea(null, () => logText)
                    )
                )
                .SetWidth(500f)
                .SetHeight(400f)
                .SetPosition(Vector2.right * 500f);
                
 
            void UpdateLogField()
            {
                logText = string.Join(Environment.NewLine, memorySink.LogEntries.Select(logEntry => logEntry.ToString()));
            }
        }
    }
}