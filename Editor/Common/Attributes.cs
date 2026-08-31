using System;

namespace FUI.Editor
{
    /// <summary>
    /// 标记程序集编译完成后由编辑器基础设施调用的静态回调方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class AssemblyCompilationFinishedAttribute : Attribute
    {
        public AssemblyCompilationFinishedAttribute() { }
    }
}
