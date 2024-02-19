using System.Collections.Generic;

namespace FUI
{
    /// <summary>
    /// View栈
    /// </summary>
    class UIStack
    {
        readonly List<UIContext> stack;

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => stack.Count;

        internal UIStack()
        {
            stack = new List<UIContext>();
        }

        /// <summary>
        /// 获取最顶端的界面上下文
        /// </summary>
        /// <returns>最顶端的上下文</returns>
        internal UIContext Peek()
        {
            if (stack.Count == 0)
            {
                return null;
            }
            return stack[stack.Count - 1];
        }

        /// <summary>
        /// 弹出最顶端的界面上下文
        /// </summary>
        /// <returns>最顶端的上下文</returns>
        internal UIContext Pop()
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
        /// 压入一个上下文
        /// </summary>
        /// <param name="context">要压入的上下文</param>
        internal void Push(UIContext context)
        {
            stack.Add(context);
        }

        /// <summary>
        /// 置顶一个上下文
        /// </summary>
        /// <param name="contextName">要指定的上下文名</param>
        /// <returns>置顶的上下文</returns>
        internal UIContext Topping(string contextName)
        {
            if(stack.Count == 0)
            {
                return null;
            }

            var last = stack[stack.Count - 1];
            if (last.Name == contextName)
            {
                return last;
            }

            var index = stack.FindIndex(item => item.Name == contextName);
            if (index < 0)
            {
                return null;
            }
            var context = stack[index];
            stack.RemoveAt(index);
            stack.Add(context);
            return context;
        }

        /// <summary>
        /// 获取一个上下文
        /// </summary>
        /// <param name="contextName">上下文名</param>
        /// <returns>要获取的上下文</returns>
        internal UIContext GetContext(string contextName)
        {
            return stack.Find(v => v.Name == contextName);
        }

        /// <summary>
        /// 移除一个上下文
        /// </summary>
        /// <param name="context">要移除的上下文</param>
        internal void Remove(UIContext context)
        {
            if (stack.Contains(context))
            {
                stack.Remove(context);
            }
        }

        /// <summary>
        /// 清空所有上下文
        /// </summary>
        internal void Clear()
        {
            stack.Clear();
        }
    }
}
