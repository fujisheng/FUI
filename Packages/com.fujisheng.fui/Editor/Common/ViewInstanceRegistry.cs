using System;
using System.Collections.Generic;
using FUI.Infrastructure;
using FUI.Rendering;
using UnityEditor;

namespace FUI.Editor
{
    /// <summary>
    /// Editor 下使用的 UI 实例注册表。
    /// 仅作为调试和工具查询索引，不参与 Player 打包，也不拥有 ViewInstance 生命周期。
    /// </summary>
    [InitializeOnLoad]
    internal static class ViewInstanceRegistry
    {
        static readonly List<WeakReference<ViewInstance>> aliveInstances = new List<WeakReference<ViewInstance>>();
        static readonly List<WeakReference<ViewInstance>> enabledInstances = new List<WeakReference<ViewInstance>>();
        static bool initialized;

        /// <summary>
        /// 当前仍存活的 UI 实例快照。
        /// </summary>
        public static IReadOnlyList<ViewInstance> AliveInstances
        {
            get
            {
                var result = new List<ViewInstance>();
                GetAliveInstances(result);
                return result;
            }
        }

        /// <summary>
        /// 当前已启用的 UI 实例快照。
        /// </summary>
        public static IReadOnlyList<ViewInstance> EnabledInstances
        {
            get
            {
                var result = new List<ViewInstance>();
                GetEnabledInstances(result);
                return result;
            }
        }

        static ViewInstanceRegistry()
        {
            Initialize();
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Initialize();
        }

        /// <summary>
        /// 将当前存活实例写入调用方提供的列表，避免工具高频调用时扫描程序集或场景对象。
        /// </summary>
        public static void GetAliveInstances(List<ViewInstance> result)
        {
            if (result == null)
            {
                return;
            }

            Initialize();
            CopyValidInstances(aliveInstances, result, ViewInstanceState.Alive);
        }

        /// <summary>
        /// 将当前启用实例写入调用方提供的列表，避免工具高频调用时扫描程序集或场景对象。
        /// </summary>
        public static void GetEnabledInstances(List<ViewInstance> result)
        {
            if (result == null)
            {
                return;
            }

            Initialize();
            CopyValidInstances(enabledInstances, result, ViewInstanceState.Alive | ViewInstanceState.Enabled);
        }

        /// <summary>
        /// 获取指定视图当前对应的已启用实例。
        /// </summary>
        public static ViewInstance GetInstance(IView view)
        {
            if (view == null)
            {
                return null;
            }

            Initialize();
            for (var index = enabledInstances.Count - 1; index >= 0; index--)
            {
                if (!enabledInstances[index].TryGetTarget(out var instance) || instance == null || !HasState(instance, ViewInstanceState.Alive | ViewInstanceState.Enabled))
                {
                    enabledInstances.RemoveAt(index);
                    continue;
                }

                if (ReferenceEquals(instance.View, view))
                {
                    return instance;
                }
            }

            return null;
        }

        static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ViewInstance.Created += Created;
            ViewInstance.Enabled += Enabled;
            ViewInstance.Disabled += Disabled;
            ViewInstance.Destroyed += Destroyed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            initialized = true;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
            {
                Clear();
            }
        }

        static void Created(ViewInstance instance)
        {
            AddInstance(aliveInstances, instance);
        }

        static void Enabled(ViewInstance instance)
        {
            AddInstance(aliveInstances, instance);
            AddInstance(enabledInstances, instance);
        }

        static void Disabled(ViewInstance instance)
        {
            RemoveInstance(enabledInstances, instance);
        }

        static void Destroyed(ViewInstance instance)
        {
            RemoveInstance(enabledInstances, instance);
            RemoveInstance(aliveInstances, instance);
        }

        static void Clear()
        {
            aliveInstances.Clear();
            enabledInstances.Clear();
        }

        static void AddInstance(List<WeakReference<ViewInstance>> collection, ViewInstance instance)
        {
            if (collection == null || instance == null)
            {
                return;
            }

            for (var index = collection.Count - 1; index >= 0; index--)
            {
                if (!collection[index].TryGetTarget(out var current) || current == null || !current.State.HasFlag(ViewInstanceState.Alive))
                {
                    collection.RemoveAt(index);
                    continue;
                }

                if (ReferenceEquals(current, instance))
                {
                    return;
                }
            }

            collection.Add(new WeakReference<ViewInstance>(instance));
        }

        static void RemoveInstance(List<WeakReference<ViewInstance>> collection, ViewInstance instance)
        {
            if (collection == null || instance == null)
            {
                return;
            }

            for (var index = collection.Count - 1; index >= 0; index--)
            {
                if (!collection[index].TryGetTarget(out var current) || current == null || ReferenceEquals(current, instance) || !current.State.HasFlag(ViewInstanceState.Alive))
                {
                    collection.RemoveAt(index);
                }
            }
        }

        static void CopyValidInstances(List<WeakReference<ViewInstance>> source, List<ViewInstance> result, ViewInstanceState requiredState)
        {
            for (var index = source.Count - 1; index >= 0; index--)
            {
                if (!source[index].TryGetTarget(out var instance) || instance == null || !HasState(instance, requiredState))
                {
                    source.RemoveAt(index);
                    continue;
                }

                result.Add(instance);
            }
        }

        static bool HasState(ViewInstance instance, ViewInstanceState requiredState)
        {
            return instance != null && (instance.State & requiredState) == requiredState;
        }
    }
}
