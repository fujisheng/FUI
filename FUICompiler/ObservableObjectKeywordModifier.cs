using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FUICompiler
{
    /// <summary>
    /// 关键字修改
    /// </summary>
    internal class ObservableObjectKeywordModifier : ITypeSyntaxNodeModifier
    {
        public SyntaxNode Modify(SyntaxNode root)
        {
            return ReplaceKeywordToPublicAndPartial(root);
        }

        /// <summary>
        /// 将所有可观察的类更改为public partial class
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        SyntaxNode ReplaceKeywordToPublicAndPartial(SyntaxNode root)
        {
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
            root = root.ReplaceNodes(classDeclarations, (oldNode, newNode) =>
            {
                if (!Utility.IsObservableObject(oldNode))
                {
                    return oldNode;
                }

                //如果是静态类或者抽象类直接不管
                if (oldNode.Modifiers.Any((k) => k.IsKind(SyntaxKind.StaticKeyword) || k.IsKind(SyntaxKind.AbstractKeyword)))
                {
                    return oldNode;
                }

                var newClass = oldNode.WithModifiers(SyntaxFactory.TokenList());
                newClass = newClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword)).NormalizeWhitespace();
                newClass = newClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)).NormalizeWhitespace();
                return newClass;
            });
            return root;
        }
    }
}
