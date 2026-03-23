using System;
using System.Collections.Generic;

namespace FUI.Manager
{
    /// <summary>
    /// 界面设置
    /// </summary>
    public readonly struct UISettings : IEquatable<UISettings>
    {
        static readonly Dictionary<Type, ITransitionProvider> transitionProviderCache = new Dictionary<Type, ITransitionProvider>();
        static readonly object cacheLock = new object();

        ///<summary>
        /// 层级
        /// </summary>
        public readonly int layer;

        ///<summary>
        /// 标记
        /// </summary>
        public readonly Attributes flag;

        /// <summary>
        /// 前置依赖
        /// </summary>
        public readonly string[] preDependency;

        /// <summary>
        /// 后置依赖
        /// </summary>
        public readonly string[] postDependency;

        /// <summary>
        /// 进入过渡
        /// </summary>
        public readonly ITransitionProvider enterTransitionProvider;

        /// <summary>
        /// 退出过渡
        /// </summary>
        public readonly ITransitionProvider exitTransitionProvider;

        public UISettings(int layer, Attributes flag, string[] preDependency, string[] postDependency, Type enterTransitionProviderType, Type exitTransitionProviderType)
        {
            this.layer = layer;
            this.flag = flag;
            this.preDependency = preDependency;
            this.postDependency = postDependency;
            this.enterTransitionProvider = null;
            this.exitTransitionProvider = null;

            if (enterTransitionProviderType != null)
            {
                this.enterTransitionProvider = GetOrCreateProvider(enterTransitionProviderType);
            }

            if (exitTransitionProviderType != null)
            {
                this.exitTransitionProvider = GetOrCreateProvider(exitTransitionProviderType);
            }
        }

        static ITransitionProvider GetOrCreateProvider(Type providerType)
        {
            if (providerType == null)
            {
                return null;
            }

            lock (cacheLock)
            {
                if (!transitionProviderCache.TryGetValue(providerType, out var provider) || provider == null)
                {
                    var instance = Activator.CreateInstance(providerType);
                    if(instance is not ITransitionProvider transitionProvider)
                    {
                        Logger.Instance.LogError($"Type {providerType.FullName} is not a valid ITransitionProvider.");
                        return null;
                    }
                    transitionProviderCache[providerType] = transitionProvider;
                    return transitionProvider;
                }

                return provider;
            }
        }

        public static UISettings Default => new UISettings((int)Layer.Common, Attributes.None, null, null, null, null);

        public static void ClearCache()
        {
            lock (cacheLock)
            {
                transitionProviderCache.Clear();
            }
        }

        public bool Equals(UISettings other)
        {
            return layer == other.layer && flag == other.flag;
        }

        public override bool Equals(object obj)
        {
            return obj is UISettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(layer, (int)flag, preDependency, postDependency, enterTransitionProvider, exitTransitionProvider);
        }
    }
}