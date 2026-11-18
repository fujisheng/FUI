using System;
using FUI.RedDot;
using UnityEngine;

namespace FUI.RedDot.UGUI
{
    /// <summary>默认红点上下文与 Unity 帧末驱动的组合根桥接。</summary>
    public static class RedDotRuntime
    {
        static RedDotCenter center;
        static RuntimeDriver driver;

        internal static event Action ContextChanged;

        /// <summary>当前是否已经配置默认中心。</summary>
        public static bool IsConfigured => center != null;

        /// <summary>返回默认中心；未配置时抛出异常。</summary>
        public static RedDotCenter Center => center ?? throw new InvalidOperationException("RedDotRuntime is not configured.");

        /// <summary>
        /// 配置或替换默认中心，并确保帧末 Tick 驱动存在。Runtime 不拥有也不会释放该中心。
        /// </summary>
        public static void Configure(RedDotCenter value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (ReferenceEquals(center, value)) return;

            center = value;
            EnsureDriver();
            driver.Attach(value);
            NotifyContextChanged();
        }

        /// <summary>尝试获取默认只读端口。</summary>
        public static bool TryGetReader(out IRedDotReader reader)
        {
            reader = center;
            return reader != null;
        }

        /// <summary>尝试获取默认中心的诊断端口。</summary>
        public static bool TryGetDiagnostics(out IRedDotDiagnostics diagnostics)
        {
            diagnostics = center;
            return diagnostics != null;
        }

        /// <summary>断开默认上下文并销毁帧驱动；不会 Dispose 中心。</summary>
        public static void Shutdown()
        {
            if (center == null && driver == null) return;
            center = null;
            try
            {
                NotifyContextChanged();
            }
            finally
            {
                if (driver != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(driver.gameObject);
                    else UnityEngine.Object.DestroyImmediate(driver.gameObject);
                }
                driver = null;
            }
        }

        static void NotifyContextChanged()
        {
            var handlers = ContextChanged;
            if (handlers == null) return;
            var invocationList = handlers.GetInvocationList();
            for (var index = 0; index < invocationList.Length; index++)
            {
                try
                {
                    ((Action)invocationList[index])();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        static void EnsureDriver()
        {
            if (driver != null) return;
            var host = new GameObject("[FUI RedDot Runtime]");
            host.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(host);
            driver = host.AddComponent<RuntimeDriver>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            center = null;
            driver = null;
            ContextChanged = null;
        }

        [DefaultExecutionOrder(32000)]
        sealed class RuntimeDriver : MonoBehaviour
        {
            RedDotCenter attachedCenter;

            public void Attach(RedDotCenter value) { attachedCenter = value; }

            void LateUpdate()
            {
                if (ReferenceEquals(center, attachedCenter)) attachedCenter.Tick();
            }
        }
    }
}
