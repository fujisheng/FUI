using System;
using System.ComponentModel;

namespace FUI.Infrastructure.Binding
{
    /// <summary>
    /// 供生成代码标记 BindingContext 对应的 ViewModel 类型。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ViewModelAttribute : Attribute
    {
        public Type Type { get; }

        public ViewModelAttribute(Type type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// 供生成代码标记 BindingContext 对应的 View 名称。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ViewAttribute : Attribute
    {
        public string ViewName { get; }

        public ViewAttribute(string viewName)
        {
            ViewName = viewName;
        }
    }
}
