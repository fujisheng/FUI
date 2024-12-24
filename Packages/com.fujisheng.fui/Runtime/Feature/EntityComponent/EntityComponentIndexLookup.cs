using System;
using System.Collections.Generic;
using System.Reflection;

namespace EntityComponent
{
    partial class EntityComponentIndexLookup
    {
        /// <summary>
        /// <entityIndex&componentType, componentIndex>
        /// </summary>
        readonly Dictionary<long, int> entityComponentIndexLookup;

        /// <summary>
        /// 组件组映射表
        /// </summary>
        readonly Dictionary<Type, ComponentIndexInfo> componentIndexInfoCache;

        static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Static;

        internal EntityComponentIndexLookup()
        {
            entityComponentIndexLookup = new Dictionary<long, int>();
            componentIndexInfoCache = new Dictionary<Type, ComponentIndexInfo>();
        }

        /// <summary>
        /// 获取或者创建一个组件索引信息
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns></returns>
        ComponentIndexInfo GetOrCreateComponentIndexInfo<T>() where T : struct, IComponent
        {
            var componentType = typeof(T);
            if(componentIndexInfoCache.TryGetValue(componentType, out var componentIndex))
            {
                return componentIndex;
            }

            var componentIndexType = typeof(ComponentIndex<T>);
            var componentIndexObj = Activator.CreateInstance(componentIndexType, true);
            var typeId = (int)(componentIndexType.GetProperty("TypeId", flags).GetValue(componentIndexObj));
            var add = componentIndexType.GetMethod("AddComponent", flags).CreateDelegate(typeof(Func<T, int>), componentIndexObj);
            var remove = componentIndexType.GetMethod("RemoveComponent", flags).CreateDelegate(typeof(Action<int>), componentIndexObj);
            var get = componentIndexType.GetMethod("GetComponent", flags).CreateDelegate(typeof(Func<int, T>), componentIndexObj);
            var replace = componentIndexType.GetMethod("ReplaceComponent", flags).CreateDelegate(typeof(Action<int, T>), componentIndexObj);
            componentIndex = new ComponentIndexInfo(componentIndexObj, typeId, add, remove, get, replace);
            componentIndexInfoCache.Add(componentType, componentIndex);
            return componentIndex;
        }

        /// <summary>
        /// 通过实体索引和组件类型获取组件唯一id
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entityIndex">实体索引</param>
        /// <returns></returns>
        long GetComponentUid<T>(int entityIndex, out ComponentIndexInfo indexInfo) where T : struct, IComponent
        {
            indexInfo = GetOrCreateComponentIndexInfo<T>();
            long uid = (long)entityIndex << 32 | indexInfo.typeId;
            return uid;
        }

        /// <summary>
        /// 通过实体索引添加一个组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityIndex"></param>
        /// <param name="component"></param>
        internal void AddComponent<T>(int entityIndex, T component) where T : struct, IComponent
        {
            var uid = GetComponentUid<T>(entityIndex, out var indexInfo);
            if(entityComponentIndexLookup.TryGetValue(uid, out var index))
            {
                throw new Exception($"entity:{entityIndex} already has component:{typeof(T)}");
            }

            entityComponentIndexLookup.Add(uid, indexInfo.Add(component));
        }

        /// <summary>
        /// 通过一个实体索引移除一个组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityIndex"></param>
        internal void RemoveComponent<T>(int entityIndex) where T : struct, IComponent
        {
            var uid = GetComponentUid<T>(entityIndex, out var indexInfo);
            if(!entityComponentIndexLookup.TryGetValue(uid, out var index))
            {
                return;
            }

            indexInfo.Remove(index);
            entityComponentIndexLookup.Remove(uid);
        }

        //internal void RemoveAllComponent(int entityIndex)
        //{
        //    foreach (var key in map.Keys)
        //    {
        //        if (key >> 32 != entityIndex)
        //        {
        //            continue;
        //        }

        //        var componentIndex = map[key];
        //        map.Remove(key);
        //        typeof(Components<>).MakeGenericType(typeof())
        //        //TODO componnents<T>.removeComponnent(componentIndex)
        //    }
        //}

        /// <summary>
        /// 通过一个实体索引获取一个组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityIndex"></param>
        /// <returns></returns>
        internal T GetComponent<T>(int entityIndex) where T : struct, IComponent
        {
            var uid = GetComponentUid<T>(entityIndex, out var indexInfo);
            if (!entityComponentIndexLookup.TryGetValue(uid, out var index))
            {
                throw new Exception($"entity:{entityIndex} dont have component : {typeof(T)}");
            }

            return indexInfo.Get<T>(index);
        }

        /// <summary>
        /// 通过实体索引判断某个实体是否拥有某个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entityIndex">实体索引</param>
        /// <returns></returns>
        internal bool HasComponent<T>(int entityIndex) where T : struct, IComponent
        {
            var uid = GetComponentUid<T>(entityIndex, out _);
            return entityComponentIndexLookup.ContainsKey(uid);
        }

        /// <summary>
        /// 替换一个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entityIndex">实体索引</param>
        /// <param name="component">组件数据</param>
        internal void ReplaceComponent<T>(int entityIndex, T component) where T : struct, IComponent
        {
            var uid = GetComponentUid<T>(entityIndex, out var indexInfo);
            if (!entityComponentIndexLookup.TryGetValue(uid, out var index))
            {
                throw new Exception($"entity:{entityIndex} dont have component : {typeof(T)}");
            }
            indexInfo.Replace(index, component);
        }
    }
}