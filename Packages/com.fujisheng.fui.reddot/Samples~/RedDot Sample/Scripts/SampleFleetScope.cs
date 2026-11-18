using FUI.RedDot;
using FUI.RedDot.UGUI;
using UnityEngine;

namespace FUI.Samples.RedDot
{
    /// <summary>主界面舰队入口跟随当前舰队，缓存恢复时重新连接，不持有Center所有权。</summary>
    sealed class SampleFleetScope : MonoBehaviour
    {
        SampleContext context;
        RedDotScope scope;
        IRedDotReader reader;

        public void Configure(SampleContext value)
        {
            context = value;
            scope = gameObject.AddComponent<RedDotScope>();
        }

        void OnEnable()
        {
            if (context == null) { return; }
            context.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (context != null) { context.Changed -= Refresh; }
        }

        void Refresh()
        {
            var next = context.FleetReader;
            if (ReferenceEquals(reader, next)) { return; }
            reader = next;
            if (next == null) { scope.Clear(); }
            else { scope.Configure(next); }
        }
    }
}
