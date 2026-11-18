extern alias Generators;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FUI.Binding;
using FUI.Navigation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using BindingSourceGenerator = Generators::FUI.Generators.BindingSourceGenerator;

namespace FUI.Portable.Tests
{
    static class GeneratedCompilation
    {
        public static Assembly Compile(params (string Path, string Text)[] sources)
        {
            var platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Where(path => !Path.GetFileName(path).StartsWith("FUI.", StringComparison.Ordinal));
            var paths = platformAssemblies.Concat(new[] { typeof(ObservableObject).Assembly.Location, typeof(Navigator).Assembly.Location, typeof(FUI.Testing.HeadlessView).Assembly.Location });
            var references = paths.Distinct().Select(path => MetadataReference.CreateFromFile(path));
            var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols("UNITY_EDITOR");
            var compilation = CSharpCompilation.Create("PortableProbe" + Guid.NewGuid().ToString("N"),
                sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, parseOptions, path: source.Path)), references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new BindingSourceGenerator().AsSourceGenerator() }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
            Assert.That(diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
            using (var stream = new MemoryStream())
            {
                var emit = output.Emit(stream);
                Assert.That(emit.Success, Is.True, "真实生成绑定无法编译：\n" +
                    string.Join("\n", emit.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)));
                return Assembly.Load(stream.ToArray());
            }
        }

        public static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Packages", "com.fujisheng.fui", "package.json")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("FUI repository root was not found.");
        }
    }
}
