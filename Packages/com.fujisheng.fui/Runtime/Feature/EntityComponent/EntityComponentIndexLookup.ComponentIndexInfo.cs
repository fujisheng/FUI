using System;

namespace EntityComponent
{
    partial class EntityComponentIndexLookup
    {
        /// <summary>
        /// 组件索引信息
        /// </summary>
        struct ComponentIndexInfo
        {
            internal readonly object componentIndex;
            internal readonly int typeId;
            readonly Delegate add;
            readonly Delegate remove;
            readonly Delegate get;
            readonly Delegate replace;

            /// <summary>
            /// 构建一个组件索引信息
            /// </summary>
            /// <param name="componentIndex">组件索引</param>
            /// <param name="typeId">组件索引类型Id</param>
            /// <param name="add">添加方法</param>
            /// <param name="remove">移除方法</param>
            /// <param name="removeAll">移除所有组件方法</param>
            /// <param name="get">获取方法</param>
            /// <param name="replace">替换方法</param>
            internal ComponentIndexInfo(object componentIndex, int typeId, Delegate add, Delegate remove, Delegate get, Delegate replace)
            {
                this.componentIndex = componentIndex;
                this.typeId = typeId;
                this.add = add;
                this.remove = remove;
                this.get = get;
                this.replace = replace;
            }

            /// <summary>
            /// 通过组件索引信息添加一个组件 并返回在其中的索引
            /// </summary>
            /// <typeparam name="T">组件类型</typeparam>
            /// <param name="component">组件数据</param>
            /// <returns>这个组件在组件索引中的索引</returns>
            internal int Add<T>(T component) where T : struct, IComponent
            {
                return (add as Func<T, int>).Invoke(component);
            }

            /// <summary>
            /// 移除某个索引的组件
            /// </summary>
            /// <param name="index">索引</param>
            internal void Remove(int index)
            {
                (remove as Action<int>).Invoke(index);
            }

            /// <summary>
            /// 获取某个索引的组件
            /// </summary>
            /// <typeparam name="T">组件类型</typeparam>
            /// <param name="index">组件在这个组件索引中的索引</param>
            /// <returns></returns>
            internal T Get<T>(int index) where T : struct, IComponent
            {
                return (get as Func<int, T>).Invoke(index);
            }

            /// <summary>
            /// 替换索引的组件
            /// </summary>
            /// <typeparam name="T">组件类型</typeparam>
            /// <param name="index">组件在这个组件索引中的索引</param>
            /// <param name="component">组件数据</param>
            internal void Replace<T>(int index, T component) where T : struct, IComponent
            {
                (replace as Action<int, T>).Invoke(index, component);
            }
        }
    }
}