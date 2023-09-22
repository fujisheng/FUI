using System.Collections.Generic;

namespace FUI
{
    /// <summary>
    /// View栈
    /// </summary>
    class ViewStack
    {
        readonly List<ViewContainer> stack;

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => stack.Count;

        internal ViewStack()
        {
            stack = new List<ViewContainer>();
        }

        /// <summary>
        /// 获取最顶端的界面容器
        /// </summary>
        /// <returns>最顶端的容器</returns>
        internal ViewContainer Peek()
        {
            if (stack.Count == 0)
            {
                return null;
            }
            return stack[stack.Count - 1];
        }

        /// <summary>
        /// 弹出最顶端的界面容器
        /// </summary>
        /// <returns>最顶端的容器</returns>
        internal ViewContainer Pop()
        {
            if (stack.Count == 0)
            {
                return null;
            }
            var view = stack[stack.Count - 1];
            stack.Remove(view);
            return view;
        }

        /// <summary>
        /// 压入一个容器
        /// </summary>
        /// <param name="container">要压入的容器</param>
        internal void Push(ViewContainer container)
        {
            stack.Add(container);
        }

        /// <summary>
        /// 置顶一个容器
        /// </summary>
        /// <param name="containerName">要指定的容器名</param>
        /// <returns>置顶的容器</returns>
        internal ViewContainer Topping(string containerName)
        {
            if(stack.Count == 0)
            {
                return null;
            }

            if (stack[stack.Count - 1].Name == containerName)
            {
                return stack[stack.Count - 1];
            }

            var index = stack.FindIndex(item => item.Name == containerName);
            if (index < 0)
            {
                return null;
            }
            var container = stack[index];
            stack.RemoveAt(index);
            stack.Add(container);
            return container;
        }

        /// <summary>
        /// 获取一个容器
        /// </summary>
        /// <param name="containerName">容器名</param>
        /// <returns>要获取的容器</returns>
        internal ViewContainer GetContainer(string containerName)
        {
            return stack.Find(v => v.Name == containerName);
        }

        /// <summary>
        /// 移除一个容器
        /// </summary>
        /// <param name="container">要移除的容器</param>
        internal void Remove(ViewContainer container)
        {
            if (stack.Contains(container))
            {
                stack.Remove(container);
            }
        }

        /// <summary>
        /// 清空所有容器
        /// </summary>
        internal void Clear()
        {
            stack.Clear();
        }
    }
}
