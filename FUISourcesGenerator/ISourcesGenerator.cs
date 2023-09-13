using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Mono.Cecil;

namespace FUISourcesGenerator
{
    internal struct Source
    {
        public readonly string name;
        public SourceText Text { get; private set; }

        public Source(string name, string text)
        {
            this.name = name;
            this.Text = SourceText.From(text);
        }

        public void BuildText(string text)
        {
            this.Text = SourceText.From(text);
        }
    }

    /// <summary>
    /// 根据类型的语法树 生成代码
    /// </summary>
    internal interface ITypeSyntaxNodeSourcesGenerator
    {
        Source?[] Generate(SyntaxNode root);
    }

    /// <summary>
    /// 类型语法树修改器
    /// </summary>
    internal interface ITypeSyntaxNodeModifier
    {
         SyntaxNode Modify(SyntaxNode root);
    }

    /// <summary>
    /// 根据类型定义生成代码
    /// </summary>
    internal interface ITypeDefinationSourcesGenerator
    {
        Source?[] GetSource(ModuleDefinition moduleDefinition, TypeDefinition typeDefinition);
    }

    /// <summary>
    /// 编译前源代码生成器
    /// </summary>
    internal interface IBeforeCompilerSourcesGenerator
    {
        Source?[] Generate();
    }

    /// <summary>
    /// 类型定义注入器
    /// </summary>
    internal interface ITypeDefinationInjector
    {
        void Inject(ModuleDefinition moduleDefinition, TypeDefinition typeDefinition);
    }
}
