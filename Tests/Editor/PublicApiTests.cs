using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FUI.Navigation;
using NUnit.Framework;

namespace FUI.Tests.Editor
{
    public sealed class PublicApiTests
    {
        static readonly string[] RuntimeAssemblies =
        {
            "FUI.Core",
            "FUI.Navigation",
            "FUI.Rendering.UGUI",
        };

        static readonly HashSet<string> ForbiddenApplicationReturnTypes = new HashSet<string>
        {
            "FUI.Infrastructure.ViewInstance",
            "FUI.Infrastructure.Binding.BindingContext",
            "FUI.Rendering.IView",
            "FUI.Rendering.IElement",
            "FUI.Rendering.UGUI.View",
            "FUI.Rendering.UGUI.Element",
        };

        static readonly string[] RemovedTypeNames =
        {
            "FUI.Rendering.UGUI.Elements.ListViewElement",
            "FUI.Rendering.UGUI.Elements.StaticListViewElement",
            "FUI.Rendering.UGUI.Elements.FixedListViewElement",
            "FUI.Rendering.UGUI.Elements.FixedPositionListViewElement",
            "FUI.Rendering.UGUI.Elements.ScrollRectElement",
            "FUI.Rendering.UGUI.Elements.EventTriggerElement",
            "FUI.Rendering.UGUI.Elements.CoordinateTransformElement",
            "FUI.Rendering.UGUI.Elements.FormatString",
            "FUI.Rendering.UGUI.Elements.IViewPrototype",
            "FUI.Rendering.UGUI.Elements.IText",
            "FUI.Rendering.UGUI.Elements.LegacyTextAdapter",
            "FUI.Rendering.UGUI.Elements.TMPTextAdapter",
        };

        [Test]
        public void PublicTypesUseStableNamespacesWithoutRedundantBrandPrefixes()
        {
            foreach (var type in GetPublicRuntimeTypes())
            {
                Assert.That(type.Namespace, Does.StartWith("FUI."), type.FullName);
                Assert.That(type.Name.StartsWith("FUI", StringComparison.Ordinal), Is.False, type.FullName);
                Assert.That(type.Name.StartsWith("UI", StringComparison.Ordinal), Is.False, type.FullName);
                Assert.That(type.Name.StartsWith("IUI", StringComparison.Ordinal), Is.False, type.FullName);
            }
        }

        [Test]
        public void NavigatorReturnTypesDoNotExposeRenderingOrInfrastructureTypes()
        {
            var methods = typeof(Navigator).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var returnType = Unwrap(method.ReturnType);
                Assert.That(ForbiddenApplicationReturnTypes, Does.Not.Contain(returnType.FullName), method.ToString());
            }
        }

        [Test]
        public void NavigatorParametersDoNotExposeRenderingOrInfrastructureTypes()
        {
            var methods = typeof(Navigator).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                foreach (var parameter in method.GetParameters())
                {
                    var parameterType = Unwrap(parameter.ParameterType);
                    Assert.That(
                        ForbiddenApplicationReturnTypes,
                        Does.Not.Contain(parameterType.FullName),
                        method.ToString());
                }
            }
        }

        [Test]
        public void RemovedElementTypesAreNotExported()
        {
            var renderingAssembly = Assembly.Load("FUI.Rendering.UGUI");
            foreach (var typeName in RemovedTypeNames)
            {
                Assert.That(renderingAssembly.GetType(typeName, throwOnError: false), Is.Null, typeName);
            }
        }

        [Test]
        public void CommonControlContractsAreAbstractAndConcreteBackendsArePublic()
        {
            var renderingAssembly = Assembly.Load("FUI.Rendering.UGUI");
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.TextElement", true).IsAbstract, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.InputFieldElement", true).IsAbstract, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.DropdownElement", true).IsAbstract, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.LegacyTextElement", true).IsPublic, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.TMPTextElement", true).IsPublic, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.LegacyInputFieldElement", true).IsPublic, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.TMPInputFieldElement", true).IsPublic, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.LegacyDropdownElement", true).IsPublic, Is.True);
            Assert.That(renderingAssembly.GetType("FUI.Rendering.UGUI.Elements.TMPDropdownElement", true).IsPublic, Is.True);
        }

        [Test]
        public void FrameworkLogAndExceptionMessagesContainNoChineseCharacters()
        {
            var packageRoot = Path.GetFullPath("Packages/com.fujisheng.fui");
            var chineseCharacters = new Regex("[\\u3400-\\u9fff]", RegexOptions.CultureInvariant);
            var messageCall = new Regex(
                @"(?:Log(?:Error|Warning|Exception)?\s*\(|throw\s+new\s+[^\s(]+\s*\()",
                RegexOptions.CultureInvariant);

            foreach (var file in Directory.EnumerateFiles(packageRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("/Tests/"))
                {
                    continue;
                }

                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;
                    if (messageCall.IsMatch(line) && chineseCharacters.IsMatch(line))
                    {
                        Assert.Fail($"Non-English framework message at {file}:{lineNumber}: {line.Trim()}");
                    }
                }
            }
        }

        [Test]
        public void ViewInstanceIsNotPublic()
        {
            var core = Assembly.Load("FUI.Core");
            var type = core.GetType("FUI.Infrastructure.ViewInstance", throwOnError: true);
            Assert.That(type.IsPublic, Is.False);
        }

        static IEnumerable<Type> GetPublicRuntimeTypes()
        {
            return RuntimeAssemblies
                .Select(Assembly.Load)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsPublic || type.IsNestedPublic);
        }

        static Type Unwrap(Type type)
        {
            if (!type.IsGenericType)
            {
                return type;
            }

            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(System.Threading.Tasks.Task<>)
                || definition == typeof(System.Threading.Tasks.ValueTask<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }
    }
}
