using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Text;

namespace FUICompiler
{
    /// <summary>
    /// 可绑定属性委托生成器
    /// </summary>
    internal class BindablePropertyDelegateGenerator : ITypeSyntaxNodeSourcesGenerator
    {
        public Source?[] Generate(SyntaxNode root)
        {
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray().Select(item =>
            {
                return item?.Name?.ToString();
            });

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
            var sources = new List<Source?>();
            foreach (var classDeclaration in classDeclarations)
            {
                if (!Utility.IsObservableObject(classDeclaration))
                {
                    continue;
                }

                var propertyDelegateBuilder = new StringBuilder();

                //添加using
                if(usings != null)
                {
                    foreach(var @using in usings)
                    {
                        propertyDelegateBuilder.AppendLine($"using {@using};");
                    }
                }

                //判断是否有命名空间
                var hasNamespace = Utility.TryGetNamespace(classDeclaration, out var namespaceName);
                if (hasNamespace)
                {
                    propertyDelegateBuilder.AppendLine($"namespace {namespaceName}");
                    propertyDelegateBuilder.AppendLine("{");
                }

                //生成分布类
                propertyDelegateBuilder.AppendLine($"public partial class {classDeclaration.Identifier.Text} : {Utility.SynchronizePropertiesFullName}");
                propertyDelegateBuilder.AppendLine("{");

                var generatedTypes = new HashSet<string>();
                var syncPropertiesBuilder = new StringBuilder();

                //遍历所有属性 生成对应委托
                foreach (var property in classDeclaration.ChildNodes().OfType<PropertyDeclarationSyntax>())
                {
                    if (!Utility.IsObservableProperty(classDeclaration, property))
                    {
                        continue;
                    }

                    var propertyType = property.Type.ToString();
                    var propertyName = property.Identifier.Text;
                    Console.WriteLine($"propertyType:{propertyType}");

                    var formattedTypeName = Utility.TypeToCSharpName(propertyType);
                    string delegateType = $"PropertyChangedHandler_{formattedTypeName}";
                    //生成对应的委托声明  这儿放在里面并且不是泛型是为了后面注入il代码的时候不用那么麻烦
                    if (!generatedTypes.Contains(delegateType))
                    {
                        propertyDelegateBuilder.AppendLine($"public delegate void {delegateType}(object sender, {propertyType} preValue, {propertyType} newValue);");
                        generatedTypes.Add(delegateType);
                    }

                    //生成对应的委托
                    var delegateName = Utility.GetPropertyChangedDelegateName(propertyName);
                    propertyDelegateBuilder.AppendLine($"public {delegateType} {delegateName};");

                    //生成对应的委托调用
                    syncPropertiesBuilder.AppendLine($"{delegateName}?.Invoke(this, this.{propertyName}, this.{propertyName});");
                }

                //生成同步所有属性的方法
                propertyDelegateBuilder.AppendLine($"void {Utility.SynchronizePropertiesFullName}.{Utility.SynchronizePropertiesMethodName}()");
                propertyDelegateBuilder.AppendLine("{");
                propertyDelegateBuilder.AppendLine(syncPropertiesBuilder.ToString());
                propertyDelegateBuilder.AppendLine("}");

                propertyDelegateBuilder.AppendLine("}");
                if (hasNamespace)
                {
                    propertyDelegateBuilder.AppendLine("}");
                }
                var code = Utility.NormalizeCode(propertyDelegateBuilder.ToString());
                Console.WriteLine($"generate property changed delegate for {classDeclaration.Identifier.Text}");
                //Console.WriteLine(code);
                sources.Add(new Source($"{classDeclaration.Identifier.Text}.PropertyChanged", code));
            }

            return sources.ToArray();
        }
    }
}
