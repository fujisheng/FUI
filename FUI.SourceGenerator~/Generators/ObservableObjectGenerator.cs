using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUI.SourceGenerator.Generators
{
    /// <summary>
    /// 可观察对象属性生成器
    /// </summary>
    [Generator]
    public class ObservableObjectGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var classDeclarations = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classDeclarations)
                {
                    var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                    if(typeSymbol == null || !typeSymbol.IsObservableObject())
                    {
                        continue;
                    }

                    GeneratePropertyChangedCode(context, compilation, classDeclaration);
                }
            }
        }

        void GeneratePropertyChangedCode(GeneratorExecutionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            if (typeSymbol == null || !typeSymbol.IsObservableObject())
            {
                return;
            }

            var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
            var className = typeSymbol.Name;
            var fullTypeName = typeSymbol.ToDisplayString();

            var codeBuilder = new StringBuilder();

            // 添加using语句
            codeBuilder.AppendLine("using FUI.Bindable;");
            codeBuilder.AppendLine("using System;");
            codeBuilder.AppendLine("using System.Collections.Generic;");
            codeBuilder.AppendLine();

            // 开始命名空间
            if (!string.IsNullOrEmpty(namespaceName))
            {
                codeBuilder.AppendLine($"namespace {namespaceName}");
                codeBuilder.AppendLine("{");
            }

            // 生成partial类
            var indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
            codeBuilder.AppendLine($"{indent}public partial class {className} : FUI.ISynchronizeProperties");
            codeBuilder.AppendLine($"{indent}{{");

            var properties = GetBindableProperties(classDeclaration, semanticModel);
            var events = GetBindableEvents(classDeclaration, semanticModel);
            var synchronizeBuilder = new StringBuilder();

            // 生成backing fields、事件委托和属性实现
            foreach (var property in properties)
            {
                var propertyName = property.propertyName;
                var propertyType = property.propertyType;
                var delegateName = Utility.GetPropertyChangedDelegateName(propertyName);

                // 生成事件委托字段
                codeBuilder.AppendLine($"{indent}    protected PropertyChangedHandler<{propertyType}> {delegateName};");

                // 统一赋值方法：在IL注入时直接调用，内部包含相等性判断、赋值、委托与通知
                codeBuilder.AppendLine($"{indent}    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                codeBuilder.AppendLine($"{indent}    protected void __Set_{propertyName}({propertyType} preValue, {propertyType} @value)");
                codeBuilder.AppendLine($"{indent}    {{");
                codeBuilder.AppendLine($"{indent}        if (System.Collections.Generic.EqualityComparer<{propertyType}>.Default.Equals(preValue, @value))");
                codeBuilder.AppendLine($"{indent}           return;");
                codeBuilder.AppendLine($"{indent}        {delegateName}?.Invoke(this, preValue, @value);");
                codeBuilder.AppendLine($"{indent}        this.OnPropertyChanged(nameof({propertyName}));");
                codeBuilder.AppendLine($"{indent}    }}");

                // 添加到同步方法
                synchronizeBuilder.AppendLine($"{indent}        {delegateName}?.Invoke(this, this.{propertyName}, this.{propertyName});");
            }
            
            // 生成事件包装方法
            foreach (var eventInfo in events)
            {
                var eventName = eventInfo.eventName;
                var parameterList = string.Join(", ", eventInfo.parameters);
                var methodName = Utility.GetEventMethodName(eventName);
                
                codeBuilder.AppendLine($"{indent}    void {methodName}({parameterList})");
                codeBuilder.AppendLine($"{indent}    {{");
                
                if (eventInfo.parameters.Length > 0)
                {
                    var paramNames = string.Join(", ", eventInfo.parameters.Select((_, i) => $"arg{i}"));
                    codeBuilder.AppendLine($"{indent}        this.{eventName}?.Invoke({paramNames});");
                }
                else
                {
                    codeBuilder.AppendLine($"{indent}        this.{eventName}?.Invoke();");
                }
                
                codeBuilder.AppendLine($"{indent}    }}");
                codeBuilder.AppendLine();
            }

            // 生成ISynchronizeProperties实现
            //codeBuilder.AppendLine($"{indent}    void FUI.ISynchronizeProperties.Synchronize()");
            var baseIsObservable = typeSymbol.BaseType != null && typeSymbol.BaseType.NeedGenerateBindingContext();
            var modify = baseIsObservable ? "override" : "virtual";
            codeBuilder.AppendLine($"{indent}    public {modify} void Synchronize()");
            codeBuilder.AppendLine($"{indent}    {{");

            if (baseIsObservable)
            {
                codeBuilder.AppendLine($"{indent}       base.Synchronize();");
            }

            codeBuilder.Append(synchronizeBuilder.ToString());
            codeBuilder.AppendLine($"{indent}    }}");

            codeBuilder.AppendLine($"{indent}}}");

            // 结束命名空间
            if (!string.IsNullOrEmpty(namespaceName))
            {
                codeBuilder.AppendLine("}");
            }

            var code = Utility.GenerateCodeWithHeader(codeBuilder.ToString());
            var formattedCode = Utility.FormatCode(code);

            // 添加生成的源文件
            var fileName = $"{fullTypeName}.PropertyChanged.g.cs";
            context.AddSource(fileName, formattedCode);
        }

        List<(string propertyName, string propertyType)> GetBindableProperties(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
        {
            var properties = new List<(string, string)>();
            var eventNames = new HashSet<string>();
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            var baseClassProperties = GetBaseClassPropertyNames(typeSymbol);

            // 首先收集所有事件字段的名称，避免重复处理
            foreach (var member in classDeclaration.Members.OfType<EventFieldDeclarationSyntax>())
            {
                foreach (var variable in member.Declaration.Variables)
                {
                    eventNames.Add(variable.Identifier.ValueText);
                }
            }

            // 收集所有事件声明的名称
            foreach (var member in classDeclaration.Members.OfType<EventDeclarationSyntax>())
            {
                eventNames.Add(member.Identifier.ValueText);
            }

            foreach (var member in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                // 检查是否有Binding特性
                if (Utility.TryGetPropertyBindingAttributes(member.AttributeLists, out _))
                {
                    var propertyName = member.Identifier.ValueText;
                    
                    // 跳过与事件同名的属性，避免冲突
                    if (eventNames.Contains(propertyName))
                    {
                        continue;
                    }
                    
                    // 跳过基类中已存在的属性，避免重复定义
                    if (baseClassProperties.Contains(propertyName))
                    {
                        continue;
                    }
                    
                    var propertyType = semanticModel.GetTypeInfo(member.Type).Type?.ToDisplayString() ?? "object";
                    properties.Add((propertyName, propertyType));
                }
            }

            return properties;
        }
        
        /// <summary>
        /// 获取基类中所有属性的名称
        /// </summary>
        HashSet<string> GetBaseClassPropertyNames(INamedTypeSymbol typeSymbol)
        {
            var propertyNames = new HashSet<string>();
            
            if (typeSymbol == null)
                return propertyNames;
                
            var baseType = typeSymbol.BaseType;
            while (baseType != null && baseType.Name != "Object")
            {
                foreach (var member in baseType.GetMembers())
                {
                    if (member is IPropertySymbol property)
                    {
                        propertyNames.Add(property.Name);
                    }
                }
                baseType = baseType.BaseType;
            }
            
            return propertyNames;
        }
        
        List<(string eventName, string[] parameters)> GetBindableEvents(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
        {
            var events = new List<(string, string[])>();
            var processedEvents = new HashSet<string>(); // 防止重复处理

            // 处理事件字段
            foreach (var member in classDeclaration.Members.OfType<EventFieldDeclarationSyntax>())
            {
                // 检查是否有Command特性
                if (Utility.TryGetCommandAttributes(member.AttributeLists, out _))
                {
                    foreach (var variable in member.Declaration.Variables)
                    {
                        var eventName = variable.Identifier.ValueText;
                        
                        // 防止重复处理同一事件
                        if (processedEvents.Contains(eventName))
                        {
                            continue;
                        }
                        processedEvents.Add(eventName);
                        
                        var parameters = ExtractEventFieldParameters(semanticModel, member);
                        events.Add((eventName, parameters));
                    }
                }
            }

            // 处理事件声明
            foreach (var member in classDeclaration.Members.OfType<EventDeclarationSyntax>())
            {
                // 检查是否有Command特性
                if (Utility.TryGetCommandAttributes(member.AttributeLists, out _))
                {
                    var eventName = member.Identifier.ValueText;
                    
                    // 防止重复处理同一事件
                    if (processedEvents.Contains(eventName))
                    {
                        continue;
                    }
                    processedEvents.Add(eventName);
                    
                    // 从事件类型中提取参数信息
                    var parameters = ExtractEventDeclarationParameters(semanticModel, member);
                    events.Add((eventName, parameters));
                }
            }

            return events;
        }
        
        string[] ExtractEventFieldParameters(SemanticModel semanticModel, EventFieldDeclarationSyntax eventField)
        {
            // 从事件类型中提取泛型参数
            var eventType = eventField.Declaration.Type;
            if (eventType is GenericNameSyntax genericType)
            {
                return genericType.TypeArgumentList.Arguments
                    .Select(arg => {
                        // 使用语义模型获取完整的类型信息
                        var typeInfo = semanticModel.GetTypeInfo(arg);
                        if (typeInfo.Type != null)
                        {
                            return typeInfo.Type.ToDisplayString();
                        }
                        return arg.ToString();
                    })
                    .Select((typeName, index) => $"{typeName} arg{index}")
                    .ToArray();
            }
            
            return new string[0];
        }
        
        string[] ExtractEventDeclarationParameters(SemanticModel semanticModel, EventDeclarationSyntax eventDeclaration)
        {
            // 从事件声明的类型中提取泛型参数
            var eventType = eventDeclaration.Type;
            if (eventType is GenericNameSyntax genericType)
            {
                return genericType.TypeArgumentList.Arguments
                    .Select((arg, index) => {
                        // 使用语义模型获取完整的类型信息
                        var typeInfo = semanticModel.GetTypeInfo(arg);
                        string typeName;
                        if (typeInfo.Type != null)
                        {
                            typeName = typeInfo.Type.ToDisplayString();
                        }
                        else
                        {
                            typeName = arg.ToString();
                        }
                        return $"{typeName} arg{index}";
                    })
                    .ToArray();
            }
            
            return new string[0];
        }
    }
}