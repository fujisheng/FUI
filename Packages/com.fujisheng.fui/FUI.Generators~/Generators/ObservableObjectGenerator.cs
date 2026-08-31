using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUI.Generators
{
    /// <summary>
    /// 可观察对象属性生成器
    /// </summary>
    public class ObservableObjectGenerator
    {
        static readonly DiagnosticDescriptor AutomaticPropertyRequiresMixedMode = new DiagnosticDescriptor(
            "FUI0013",
            "Auto-properties require Mixed mode",
            "Bound auto-property '{0}' requires ViewModule Mixed mode; use an [ObservableProperty] field in Pure mode",
            "FUI.Generation",
            DiagnosticSeverity.Error,
            true);

        /// <summary>
        /// 描述需要生成通知逻辑的属性或字段成员。
        /// </summary>
        sealed class ObservableMember
        {
            public string PropertyName;
            public string PropertyType;
            public string FieldName;
            public bool GeneratesProperty;
        }

        internal void Generate(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            GeneratePropertyChangedCode(context, compilation, classDeclaration);
        }

        void GeneratePropertyChangedCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            if (typeSymbol == null || !typeSymbol.IsObservableObject())
            {
                return;
            }

            var declarations = Utility.GetSourceDeclarations(typeSymbol);
            ValidatePropertyMode(context, compilation, declarations);

            var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
            var className = typeSymbol.Name;
            var fullTypeName = typeSymbol.ToDisplayString();

            var codeBuilder = new StringBuilder();

            // 添加using语句
            codeBuilder.AppendLine("using FUI.Binding;");
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
            codeBuilder.AppendLine($"{indent}public partial class {className} : FUI.Presentation.ISynchronizeProperties");
            codeBuilder.AppendLine($"{indent}{{");

            var properties = GetObservableMembers(declarations, compilation, typeSymbol);
            var events = GetBindableEvents(declarations, compilation);
            var synchronizeBuilder = new StringBuilder();

            // 生成backing fields、事件委托和属性实现
            foreach (var property in properties)
            {
                var propertyName = property.PropertyName;
                var propertyType = property.PropertyType;
                var delegateName = Utility.GetPropertyChangedDelegateName(propertyName);

                if (property.GeneratesProperty)
                {
                    codeBuilder.AppendLine($"{indent}    public {propertyType} {propertyName}");
                    codeBuilder.AppendLine($"{indent}    {{");
                    codeBuilder.AppendLine($"{indent}        get => this.{property.FieldName};");
                    codeBuilder.AppendLine($"{indent}        set");
                    codeBuilder.AppendLine($"{indent}        {{");
                    codeBuilder.AppendLine($"{indent}            var previous = this.{property.FieldName};");
                    codeBuilder.AppendLine($"{indent}            if (System.Collections.Generic.EqualityComparer<{propertyType}>.Default.Equals(previous, value)) return;");
                    codeBuilder.AppendLine($"{indent}            this.{property.FieldName} = value;");
                    codeBuilder.AppendLine($"{indent}            this.__Set_{propertyName}(previous, value);");
                    codeBuilder.AppendLine($"{indent}        }}");
                    codeBuilder.AppendLine($"{indent}    }}");
                }

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
            //codeBuilder.AppendLine($"{indent}    void FUI.Presentation.ISynchronizeProperties.Synchronize()");
            // Synchronize 属于可观察数据继承链，不取决于基类是否声明 View Binding。
            // 例如抽象列表项没有绑定特性，但仍会生成 virtual Synchronize，派生项必须 override。
            var baseIsObservable = typeSymbol.BaseType != null
                && typeSymbol.BaseType.DeclaringSyntaxReferences.Length > 0
                && typeSymbol.BaseType.IsObservableObject();
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

        static void ValidatePropertyMode(
            SourceProductionContext context,
            Compilation compilation,
            IReadOnlyList<ClassDeclarationSyntax> declarations)
        {
            var module = compilation.Assembly.GetAttributes().FirstOrDefault(attribute =>
                attribute.AttributeClass?.ToDisplayString() == "FUI.Binding.ViewModuleAttribute");
            var mixed = module?.NamedArguments.Any(pair =>
                pair.Key == "PropertyMode" && Convert.ToInt32(pair.Value.Value) == 1) == true;
            if (mixed)
            {
                return;
            }

            foreach (var classDeclaration in declarations)
            {
                foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (!Utility.TryGetPropertyBindingAttributes(property.AttributeLists, out _)
                        || property.AccessorList == null
                        || property.AccessorList.Accessors.Any(accessor =>
                            accessor.Body != null || accessor.ExpressionBody != null))
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                        AutomaticPropertyRequiresMixedMode,
                        property.Identifier.GetLocation(),
                        property.Identifier.ValueText));
                }
            }
        }

        List<ObservableMember> GetObservableMembers(
            IReadOnlyList<ClassDeclarationSyntax> declarations,
            Compilation compilation,
            INamedTypeSymbol typeSymbol)
        {
            var properties = new List<ObservableMember>();
            var eventNames = new HashSet<string>();
            var baseClassProperties = GetBaseClassPropertyNames(typeSymbol);

            // 首先收集所有事件字段的名称，避免重复处理
            foreach (var classDeclaration in declarations)
            {
                foreach (var member in classDeclaration.Members.OfType<EventFieldDeclarationSyntax>())
                {
                    foreach (var variable in member.Declaration.Variables)
                    {
                        eventNames.Add(variable.Identifier.ValueText);
                    }
                }

                foreach (var member in classDeclaration.Members.OfType<EventDeclarationSyntax>())
                {
                    eventNames.Add(member.Identifier.ValueText);
                }
            }

            var declaredPropertyNames = new HashSet<string>(
                declarations.SelectMany(declaration => declaration.Members.OfType<PropertyDeclarationSyntax>())
                    .Select(member => member.Identifier.ValueText));
            foreach (var classDeclaration in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                foreach (var member in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (!Utility.TryGetPropertyBindingAttributes(member.AttributeLists, out _))
                    {
                        continue;
                    }

                    var propertyName = member.Identifier.ValueText;
                    if (eventNames.Contains(propertyName) || baseClassProperties.Contains(propertyName))
                    {
                        continue;
                    }

                    var propertyType = semanticModel.GetTypeInfo(member.Type).Type?.ToDisplayString() ?? "object";
                    properties.Add(new ObservableMember
                    {
                        PropertyName = propertyName,
                        PropertyType = propertyType,
                    });
                }

                foreach (var field in classDeclaration.Members.OfType<FieldDeclarationSyntax>())
                {
                    var isObservable = Utility.HasAttribute(
                        field.AttributeLists,
                        "ObservableProperty",
                        "ObservablePropertyAttribute");
                    if (!isObservable || Utility.HasAttribute(
                            field.AttributeLists,
                            "IgnoreObservable",
                            "IgnoreObservableAttribute"))
                    {
                        continue;
                    }

                    var propertyType = semanticModel.GetTypeInfo(field.Declaration.Type).Type?.ToDisplayString()
                        ?? field.Declaration.Type.ToString();
                    foreach (var variable in field.Declaration.Variables)
                    {
                        var fieldName = variable.Identifier.ValueText;
                        var propertyName = ToPropertyName(fieldName);
                        if (string.IsNullOrEmpty(propertyName)
                            || declaredPropertyNames.Contains(propertyName)
                            || baseClassProperties.Contains(propertyName))
                        {
                            continue;
                        }

                        declaredPropertyNames.Add(propertyName);
                        properties.Add(new ObservableMember
                        {
                            PropertyName = propertyName,
                            PropertyType = propertyType,
                            FieldName = fieldName,
                            GeneratesProperty = true,
                        });
                    }
                }
            }

            return properties;
        }

        static string ToPropertyName(string fieldName)
        {
            var trimmed = fieldName?.TrimStart('_');
            if (string.IsNullOrEmpty(trimmed))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
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

        List<(string eventName, string[] parameters)> GetBindableEvents(
            IReadOnlyList<ClassDeclarationSyntax> declarations,
            Compilation compilation)
        {
            var events = new List<(string, string[])>();
            var processedEvents = new HashSet<string>(); // 防止重复处理

            foreach (var classDeclaration in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                foreach (var member in classDeclaration.Members.OfType<EventFieldDeclarationSyntax>())
                {
                    if (!Utility.TryGetCommandAttributes(member.AttributeLists, out _))
                    {
                        continue;
                    }

                    foreach (var variable in member.Declaration.Variables)
                    {
                        var eventName = variable.Identifier.ValueText;
                        if (!processedEvents.Add(eventName))
                        {
                            continue;
                        }

                        var parameters = ExtractEventFieldParameters(semanticModel, member);
                        events.Add((eventName, parameters));
                    }
                }

                foreach (var member in classDeclaration.Members.OfType<EventDeclarationSyntax>())
                {
                    if (!Utility.TryGetCommandAttributes(member.AttributeLists, out _))
                    {
                        continue;
                    }

                    var eventName = member.Identifier.ValueText;
                    if (!processedEvents.Add(eventName))
                    {
                        continue;
                    }

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
