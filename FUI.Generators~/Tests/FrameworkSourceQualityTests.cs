using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FUI.Generators.Tests
{
    /// <summary>
    /// 验证 FUI 生产源码的全球化文本和类型文档约束。
    /// </summary>
    public sealed class FrameworkSourceQualityTests
    {
        [Test]
        public void ProductionStringLiteralsDoNotContainChineseCharacters()
        {
            var violations = new List<string>();
            foreach (var sourcePath in EnumerateProductionSources())
            {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), path: sourcePath);
                foreach (var token in tree.GetRoot().DescendantTokens())
                {
                    // LiteralExpressionSyntax 同时覆盖普通、UTF-8 与 raw 字符串，避免新增语法绕过门禁。
                    if (token.Parent is not LiteralExpressionSyntax
                        && !token.IsKind(SyntaxKind.InterpolatedStringTextToken))
                    {
                        continue;
                    }

                    if (!token.ValueText.Any(IsChineseCharacter))
                    {
                        continue;
                    }

                    var line = tree.GetLineSpan(token.Span).StartLinePosition.Line + 1;
                    violations.Add($"{RelativePath(sourcePath)}:{line}: {token.ValueText}");
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                "Framework production string literals must be English:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ProductionTypeDeclarationsHaveNonEmptyXmlSummaries()
        {
            var violations = new List<string>();
            foreach (var sourcePath in EnumerateProductionSources())
            {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), path: sourcePath);
                var declarations = tree.GetRoot().DescendantNodes().Where(node =>
                    node is BaseTypeDeclarationSyntax || node is DelegateDeclarationSyntax);
                foreach (var declaration in declarations)
                {
                    if (HasMeaningfulSummary(declaration))
                    {
                        continue;
                    }

                    var line = tree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1;
                    violations.Add($"{RelativePath(sourcePath)}:{line}: {DeclarationName(declaration)}");
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                "Framework type declarations require a meaningful XML summary:\n" + string.Join("\n", violations));
        }

        static IEnumerable<string> EnumerateProductionSources()
        {
            var packageRoot = FindPackageRoot();
            var roots = new[]
            {
                Path.Combine(packageRoot, "Runtime"),
                Path.Combine(packageRoot, "Editor"),
                Path.Combine(packageRoot, "FUI.Generators~", "Analyzers"),
                Path.Combine(packageRoot, "FUI.Generators~", "Core"),
                Path.Combine(packageRoot, "FUI.Generators~", "Generators"),
            };

            return roots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .OrderBy(path => path, StringComparer.Ordinal);
        }

        static string FindPackageRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "package.json");
                if (File.Exists(candidate))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the FUI package root.");
        }

        static bool HasMeaningfulSummary(SyntaxNode declaration)
        {
            return declaration.GetLeadingTrivia()
                .Select(trivia => trivia.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .SelectMany(comment => comment.Content.OfType<XmlElementSyntax>())
                .Where(element => element.StartTag.Name.LocalName.Text == "summary")
                .SelectMany(element => element.Content)
                .Any(content => content.ToString().Any(char.IsLetterOrDigit));
        }

        static string DeclarationName(SyntaxNode declaration)
        {
            if (declaration is BaseTypeDeclarationSyntax type)
            {
                return type.Identifier.ValueText;
            }
            return ((DelegateDeclarationSyntax)declaration).Identifier.ValueText;
        }

        static bool IsChineseCharacter(char value)
        {
            return (value >= '\u3400' && value <= '\u4DBF')
                || (value >= '\u4E00' && value <= '\u9FFF')
                || (value >= '\uF900' && value <= '\uFAFF');
        }

        static string RelativePath(string path)
        {
            var packageRoot = FindPackageRoot();
            return path.Substring(packageRoot.Length + 1).Replace('\\', '/');
        }
    }
}
