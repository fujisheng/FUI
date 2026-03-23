using System;

namespace FUI.Editor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AssemblyCompilationFinishedAttribute : Attribute
    {
        public AssemblyCompilationFinishedAttribute() { }
    }
}