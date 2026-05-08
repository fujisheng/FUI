using System;
using System.Collections.Generic;
using UnityEditor;

namespace FUI.Editor
{
    /// <summary>
    /// Editor 下使用的 UI 实体注册表。
    /// 仅作为调试和工具查询索引，不参与 Player 打包，也不拥有 UIEntity 生命周期。
    /// </summary>
    [InitializeOnLoad]
    public static class UIEntityRegistry
    {
        static readonly List<WeakReference<UIEntity>> aliveEntities = new List<WeakReference<UIEntity>>();
        static readonly List<WeakReference<UIEntity>> enabledEntities = new List<WeakReference<UIEntity>>();
        static bool initialized;

        /// <summary>
        /// 当前仍存活的 UI 实体快照。
        /// </summary>
        public static IReadOnlyList<UIEntity> AliveEntities
        {
            get
            {
                var result = new List<UIEntity>();
                GetAliveEntities(result);
                return result;
            }
        }

        /// <summary>
        /// 当前已启用的 UI 实体快照。
        /// </summary>
        public static IReadOnlyList<UIEntity> EnabledEntities
        {
            get
            {
                var result = new List<UIEntity>();
                GetEnabledEntities(result);
                return result;
            }
        }

        static UIEntityRegistry()
        {
            Initialize();
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            Initialize();
        }

        /// <summary>
        /// 将当前存活实体写入调用方提供的列表，避免工具高频调用时扫描程序集或场景对象。
        /// </summary>
        public static void GetAliveEntities(List<UIEntity> result)
        {
            if (result == null)
            {
                return;
            }

            Initialize();
            CopyValidEntities(aliveEntities, result, UIEntityState.Alive);
        }

        /// <summary>
        /// 将当前启用实体写入调用方提供的列表，避免工具高频调用时扫描程序集或场景对象。
        /// </summary>
        public static void GetEnabledEntities(List<UIEntity> result)
        {
            if (result == null)
            {
                return;
            }

            Initialize();
            CopyValidEntities(enabledEntities, result, UIEntityState.Alive | UIEntityState.Enabled);
        }

        /// <summary>
        /// 获取指定视图当前对应的已启用实体。
        /// </summary>
        public static UIEntity GetEntity(IView view)
        {
            if (view == null)
            {
                return null;
            }

            Initialize();
            for (var index = enabledEntities.Count - 1; index >= 0; index--)
            {
                if (!enabledEntities[index].TryGetTarget(out var entity) || entity == null || !HasState(entity, UIEntityState.Alive | UIEntityState.Enabled))
                {
                    enabledEntities.RemoveAt(index);
                    continue;
                }

                if (ReferenceEquals(entity.View, view))
                {
                    return entity;
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

            UIEntity.OnEntityCreated += OnEntityCreated;
            UIEntity.OnEntityEnabled += OnEntityEnabled;
            UIEntity.OnEntityDisabled += OnEntityDisabled;
            UIEntity.OnEntityDestoryed += OnEntityDestoryed;
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

        static void OnEntityCreated(UIEntity entity)
        {
            AddEntity(aliveEntities, entity);
        }

        static void OnEntityEnabled(UIEntity entity)
        {
            AddEntity(aliveEntities, entity);
            AddEntity(enabledEntities, entity);
        }

        static void OnEntityDisabled(UIEntity entity)
        {
            RemoveEntity(enabledEntities, entity);
        }

        static void OnEntityDestoryed(UIEntity entity)
        {
            RemoveEntity(enabledEntities, entity);
            RemoveEntity(aliveEntities, entity);
        }

        static void Clear()
        {
            aliveEntities.Clear();
            enabledEntities.Clear();
        }

        static void AddEntity(List<WeakReference<UIEntity>> collection, UIEntity entity)
        {
            if (collection == null || entity == null)
            {
                return;
            }

            for (var index = collection.Count - 1; index >= 0; index--)
            {
                if (!collection[index].TryGetTarget(out var current) || current == null || !current.State.HasFlag(UIEntityState.Alive))
                {
                    collection.RemoveAt(index);
                    continue;
                }

                if (ReferenceEquals(current, entity))
                {
                    return;
                }
            }

            collection.Add(new WeakReference<UIEntity>(entity));
        }

        static void RemoveEntity(List<WeakReference<UIEntity>> collection, UIEntity entity)
        {
            if (collection == null || entity == null)
            {
                return;
            }

            for (var index = collection.Count - 1; index >= 0; index--)
            {
                if (!collection[index].TryGetTarget(out var current) || current == null || ReferenceEquals(current, entity) || !current.State.HasFlag(UIEntityState.Alive))
                {
                    collection.RemoveAt(index);
                }
            }
        }

        static void CopyValidEntities(List<WeakReference<UIEntity>> source, List<UIEntity> result, UIEntityState requiredState)
        {
            for (var index = source.Count - 1; index >= 0; index--)
            {
                if (!source[index].TryGetTarget(out var entity) || entity == null || !HasState(entity, requiredState))
                {
                    source.RemoveAt(index);
                    continue;
                }

                result.Add(entity);
            }
        }

        static bool HasState(UIEntity entity, UIEntityState requiredState)
        {
            return entity != null && (entity.State & requiredState) == requiredState;
        }
    }
}
