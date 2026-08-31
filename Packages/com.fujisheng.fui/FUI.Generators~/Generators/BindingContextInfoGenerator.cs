using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace FUI.Generators
{
    /// <summary>
    /// 绑定上下文信息生成器
    /// </summary>
    public partial class BindingContextInfoGenerator
    {
        /// <summary>
        /// 初始化增量生成器
        /// </summary>
        internal void Generate(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            var generator = new ContextInfoByAttributeGenerator();
            var bindingInfos = generator.Generate(compilation, classDeclaration);
            foreach (var bindingInfo in bindingInfos)
            {
                if (bindingInfo == null)
                {
                    continue;
                }

                var manifestNamespace = $"{Utility.GetModuleNamespace(compilation)}.Infrastructure.BindingManifests";
                var code = BuildContextInfoCode(bindingInfo, manifestNamespace);
                code = Utility.FormatCode(code);
                var viewNameFormat = string.IsNullOrEmpty(bindingInfo.viewName) ? "Empty" : bindingInfo.viewName;
                var fileName = $"{bindingInfo.viewModelType}.{viewNameFormat}.BindingInfo.g";
                context.AddSource(fileName, code);
            }
        }
    }
}
