using System;

namespace FUI.Navigation
{
    /// <summary>
    /// 页面导航、缓存、依赖和动画的编译期声明。
    /// 仅供 Source Generator 读取；运行时使用生成的 RoutePolicy，不反射该 Attribute。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RoutePolicyAttribute : Attribute
    {
        public int Layer { get; }
        public Type EnterTransitionProvider { get; set; }
        public Type ExitTransitionProvider { get; set; }

        /// <summary>提示覆盖层设为 false，保持当前页面的焦点和交互。</summary>
        public bool TakeFocus { get; set; } = true;
        public bool AllowMultiple { get; set; }
        public HistoryMode HistoryMode { get; set; } = HistoryMode.Stack;
        public CoverageMode CoverageMode { get; set; } = CoverageMode.KeepVisible;
        public InputCoverageMode InputCoverage { get; set; } = InputCoverageMode.Default;
        public CacheMode CacheMode { get; set; } = CacheMode.None;
        public int CacheCapacity { get; set; } = 1;
        public float CacheDuration { get; set; }

        /// <summary>
        /// 父页面创建前必须成功打开的默认 Route，以 ViewModel 类型声明，避免手写页面名。
        /// </summary>
        public Type[] RequiredBefore { get; set; }

        /// <summary>
        /// 父页面创建后附加打开的默认 Route，以 ViewModel 类型声明，避免手写页面名。
        /// </summary>
        public Type[] AttachedAfter { get; set; }

        public RoutePolicyAttribute(Layer layer = FUI.Navigation.Layer.Panel)
            : this((int)layer)
        {
        }

        public RoutePolicyAttribute(int layer)
        {
            Layer = layer;
        }
    }
}
