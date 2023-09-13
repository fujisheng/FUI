using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FUISourcesGenerator
{
    /// <summary>
    /// 关键字修改
    /// </summary>
    internal class KeywordModifier : ITypeSyntaxNodeModifier
    {
        public SyntaxNode Modify(SyntaxNode root)
        {
            return ReplaceKeywordToPublicAndPartial(root);
        }

        /// <summary>
        /// 将所有拥有ObserableObject这个特性的类更改为public partial class
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        SyntaxNode ReplaceKeywordToPublicAndPartial(SyntaxNode root)
        {
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
            foreach (var classDeclaration in classDeclarations)
            {
                if (!Utility.HasObservableObjectAttribute(classDeclaration))
                {
                    continue;
                }

                //如果是静态类或者抽象类直接不管
                if(classDeclaration.Modifiers.Any((k)=>k.IsKind(SyntaxKind.StaticKeyword) || k.IsKind(SyntaxKind.AbstractKeyword)))
                {
                    continue;
                }

                var newClass = classDeclaration.WithModifiers(SyntaxFactory.TokenList());
                newClass = newClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword)).NormalizeWhitespace();
                newClass = newClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)).NormalizeWhitespace();
                root = root.ReplaceNode(classDeclaration, newClass);
                Console.WriteLine($"replace {classDeclaration.Identifier} to public and partial");
            }
            
            return root;
        }
    }
}
