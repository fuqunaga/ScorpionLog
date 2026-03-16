using UnityEngine;

namespace CategorizedLogging
{
    /// <summary>
    /// Component to hold log settings in the scene
    /// </summary>
    public abstract class SinkConfigMonoBehaviourBase<TSink> : MonoBehaviour
        where TSink : ISink, new()
    {
        public virtual TSink Sink { get; } = new();

        public abstract LogFilter LogFilter { get; }
        
        
        #region Unity
        
        protected virtual void OnEnable() => Register();

        protected virtual void OnDisable() => Unregister();

        protected virtual void OnValidate()
        {
            // OnValidateはドメインリロード時に呼ばれるが、シーンロード時に別のオブジェクトとして再度呼ばれる
            // ドメインリロード時のOnValidateは無視したいので、Application.isPlayingを確認する
            if (!isActiveAndEnabled || !Application.isPlaying)
            {
                return;
            }
            
            Register();
        }
        
        #endregion


        protected virtual void Register()
        {
            if (Sink is not {} sink)
            {
                return;
            }
            
            Log.RegisterSink(sink, LogFilter);
        }
        
        protected virtual void Unregister()
        {
            if (Sink is not {} sink)
            {
                return;
            }
            
            Log.UnregisterSink(sink);
        }
    }
}