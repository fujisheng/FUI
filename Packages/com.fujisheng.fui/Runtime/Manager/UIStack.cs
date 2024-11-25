using System.Collections.Generic;

namespace FUI.Manager
{
    /// <summary>
    /// View栈
    /// </summary>
    class UIStack
    {
        readonly List<UIEntity> stack;

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => stack.Count;

        /// <summary>
        /// 所有Item
        /// </summary>
        internal IReadOnlyList<UIEntity> Items => stack;

        internal UIStack()
        {
            stack = new List<UIEntity>();
        }

        internal UIEntity this[int index]=>stack[index];

        /// <summary>
        /// 获取最顶端的界面UI实体
        /// </summary>
        /// <returns>最顶端的UI实体</returns>
        internal UIEntity Peek()
        {
            if (stack.Count == 0)
            {
                return null;
            }
            return stack[stack.Count - 1];
        }

        /// <summary>
        /// 弹出最顶端的UI实体
        /// </summary>
        /// <returns>最顶端的UI实体</returns>
        internal UIEntity Pop()
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
        /// 压入一个UI实体
        /// </summary>
        /// <param name="entity">要压入的UI实体</param>
        internal void Push(UIEntity entity)
        {
            stack.Add(entity);
        }

        /// <summary>
        /// 置顶一个UI实体
        /// </summary>
        /// <param name="entityName">要指定的UI实体名</param>
        /// <returns>置顶的UI实体</returns>
        internal UIEntity Topping(string entityName)
        {
            if(stack.Count == 0)
            {
                return null;
            }

            var peek = stack[stack.Count - 1];
            if(peek.Name == entityName)
            {
                return peek;
            }

            var index = stack.FindIndex(item => item.Name == entityName);
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
        /// 获取一个UI实体
        /// </summary>
        /// <param name="entityName">UI实体名</param>
        /// <returns>要获取的UI实体</returns>
        internal UIEntity GetUIEntity(string entityName)
        {
            return stack.Find(v => v.Name == entityName);
        }

        /// <summary>
        /// 移除一个UI实体
        /// </summary>
        /// <param name="entity">要移除的UI实体</param>
        internal void Remove(UIEntity entity)
        {
            if (stack.Contains(entity))
            {
                stack.Remove(entity);
            }
        }

        /// <summary>
        /// 清空所有UI实体
        /// </summary>
        internal void Clear()
        {
            stack.Clear();
        }
    }
}
