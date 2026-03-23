using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace FUI.SourceGenerator
{
    /// <summary>
    /// 上下文信息生成器接口
    /// </summary>
    public interface IContextInfoGenerator
    {
        /// <summary>
        /// 为特定类生成绑定上下文信息
        /// </summary>
        /// <param name="context">生成器执行上下文</param>
        /// <param name="compilation">编译信息</param>
        /// <param name="classDeclaration">类声明</param>
        /// <returns>绑定上下文信息列表</returns>
        List<ContextBindingInfo> Generate(GeneratorExecutionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration);
    }
}