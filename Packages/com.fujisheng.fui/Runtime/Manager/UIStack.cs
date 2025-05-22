using System.Collections.Generic;
using System.Text;

namespace FUI.Manager
{
    struct UIStackItem
    {
        /// <summary>
        /// UI实体
        /// </summary>
        internal readonly UIEntity Entity;

        /// <summary>
        /// 父级 被谁依赖
        /// </summary>
        internal readonly string ParentViewName;

        internal UIStackItem(UIEntity entity, string parent)
        {
            Entity = entity;
            ParentViewName = parent;
        }
    }

    /// <summary>
    /// View栈
    /// </summary>
    class UIStack
    {
        readonly List<UIStackItem> stack;

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => stack.Count;

        /// <summary>
        /// 所有Item
        /// </summary>
        internal IReadOnlyList<UIStackItem> Items => stack;

        internal UIStack()
        {
            stack = new List<UIStackItem>();
        }

        internal UIStackItem this[int index] => stack[index];

        /// <summary>
        /// 获取最顶端的界面UI实体
        /// </summary>
        /// <returns>最顶端的UI实体</returns>
        internal UIStackItem? Peek()
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
        internal UIStackItem? Pop()
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
        internal void Push(UIEntity entity, string parent = null)
        {
            stack.Add(new UIStackItem(entity, parent));
        }

        /// <summary>
        /// 置顶一个UI实体
        /// </summary>
        /// <param name="entityName">要指定的UI实体名</param>
        /// <returns>置顶的UI实体</returns>
        internal bool Topping(UIEntity entity, out UIEntity lastPeek)
        {
            lastPeek = null;

            if (stack.Count == 0)
            {
                return false;
            }

            var peek = Peek();
            lastPeek = peek.Value.Entity;
            if(lastPeek == entity)
            {
                return false;
            }

            if (stack.Remove(peek.Value))
            {
                stack.Add(peek.Value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取一个UI实体
        /// </summary>
        /// <param name="entityName">UI实体名</param>
        /// <returns>要获取的UI实体</returns>
        internal UIEntity GetUIEntity(string entityName)
        {
            return stack.Find(v => v.Entity.Name == entityName).Entity;
        }

        /// <summary>
        /// 是否包含某个UI实体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal bool Exist(UIEntity entity)
        {
            return stack.Exists(v => v.Entity == entity);
        }

        /// <summary>
        /// 移除一个UI实体
        /// </summary>
        /// <param name="entity">要移除的UI实体</param>
        internal void Remove(UIEntity entity)
        {
            stack.RemoveAll(item => item.Entity == entity);
        }

        /// <summary>
        /// 清空所有UI实体
        /// </summary>
        internal void Clear()
        {
            stack.Clear();
        }

        public override string ToString()
        {
            if (stack == null)
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("UIStack");
            stringBuilder.AppendLine("\t{");
            for (int i = 0; i < stack.Count; i++)
            {
                var item = stack[i];
                stringBuilder.AppendLine($"\t\t[{item.Entity}] Parent[{item.ParentViewName}]");
            }
            stringBuilder.AppendLine("\t}");
            return stringBuilder.ToString();
        }
    }
}
