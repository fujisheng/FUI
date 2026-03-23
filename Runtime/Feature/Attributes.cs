using System;

namespace Feature
{
    /// <summary>
    /// 标记一个Model在初始化时自动构造
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AutoConstructOnInitializationAttribute : Attribute { }

    public abstract class GroupAttribute : Attribute { }

    /// <summary>
    /// 标记一个Model属于公共组
    /// </summary>
    public class  PublicGroup : GroupAttribute { }
}
