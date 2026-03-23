using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FUI.SourceGenerator
{
    /// <summary>
    /// 通过特性分析生成绑定上下文信息
    /// </summary>
    public class ContextInfoByAttributeGenerator : IContextInfoGenerator
    {
        public List<ContextBindingInfo> Generate(GeneratorExecutionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            var contexts = new List<ContextBindingInfo>();
            if (typeSymbol == null || !typeSymbol.IsObservableObject())
            {
                return contexts;
            }

            Utility.TryGetViewBindingAttribute(classDeclaration.AttributeLists, out var attribute);
            contexts.Add(CreateContext(semanticModel, classDeclaration, attribute));
            return contexts;
        }

        /// <summary>
        /// 创建一个绑定上下文配置
        /// </summary>
        /// <param name="bindingInfo">配置</param>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="classDeclaration">类定义节点</param>
        /// <param name="attribute">特性节点</param>
        ContextBindingInfo CreateContext(SemanticModel semanticModel, ClassDeclarationSyntax classDeclaration, AttributeSyntax attribute)
        {
            var bindingContext = new ContextBindingInfo();

            var type = semanticModel.GetDeclaredSymbol(classDeclaration);

            bindingContext.viewModelType = type.ToString();
            bindingContext.viewModelNamespace = type.ContainingNamespace?.ToString();
            var baseType = type.BaseType;
            if(baseType != null && baseType.NeedGenerateBindingContext())
            {
                bindingContext.baseViewModelType = baseType.ToString();
                bindingContext.baseViewName = baseType.GetAttributes()
                    .FirstOrDefault((item) => item.AttributeClass.ToString() == "FUI.ViewBindingAttribute" || item.AttributeClass.ToString() == "FUI.ViewBinding")
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString();
                bindingContext.baseViewModelNamespace = baseType.ContainingNamespace?.ToString();
            }

            if (attribute == null)
            {
                bindingContext.viewName = string.Empty;
            }
            else
            {
                // 允许常量字符串或字面量作为视图名称
                var firstArg = attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0
                    ? attribute.ArgumentList.Arguments[0].Expression
                    : null;
                var constViewName = GetStringConstant(semanticModel, firstArg);
                bindingContext.viewName = constViewName ?? string.Empty;
            }

            foreach (var property in classDeclaration.ChildNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (!Utility.TryGetPropertyBindingAttributes(property.AttributeLists, out var propertyAttributes))
                {
                    continue;
                }

                foreach (var propertyAttribute in propertyAttributes)
                {
                    var propertyBindingInfo = CreatePropertyBindingInfo(semanticModel, classDeclaration, property, propertyAttribute);
                    if (propertyBindingInfo != null)
                    {
                        bindingContext.properties.Add(propertyBindingInfo);
                    }
                }
            }

            foreach (var member in classDeclaration.ChildNodes().OfType<MemberDeclarationSyntax>())
            {
                if (!Utility.TryGetCommandAttributes(member.AttributeLists, out var commandAttributes))
                {
                    continue;
                }

                foreach (var commandAttribute in commandAttributes)
                {
                    var commandBindingInfo = CreateCommand(semanticModel, classDeclaration, member, commandAttribute);
                    if (commandBindingInfo != null)
                    {
                        bindingContext.commands.Add(commandBindingInfo);
                    }
                }
            }

            return bindingContext;
        }

        /// <summary>
        /// 创建属性绑定配置文件
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="clazz">类定义节点</param>
        /// <param name="property">属性定义节点</param>
        /// <param name="propertyAttribute">属性特性节点</param>
        /// <returns></returns>
        PropertyBindingInfo CreatePropertyBindingInfo(SemanticModel semanticModel, ClassDeclarationSyntax clazz, PropertyDeclarationSyntax property, AttributeSyntax propertyAttribute)
        {
            var propertyInfo = CreatePropertyInfo(semanticModel, clazz, property, propertyAttribute);
            var converterInfo = CreateConverterInfo(semanticModel, clazz, property, propertyAttribute);
            var targetInfo = CreateTargetInfo(semanticModel, clazz, property, propertyAttribute);
            var bindingMode = CreateBindingModeInfo(semanticModel, clazz, property, propertyAttribute);

            if (propertyInfo == null || targetInfo == null)
            {
                return null;
            }

            return new PropertyBindingInfo
            {
                propertyInfo = propertyInfo,
                converterInfo = converterInfo,
                targetInfo = targetInfo,
                bindingMode = bindingMode,
            };
        }

        /// <summary>
        /// 创建属性信息
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="clazz">类定义节点</param>
        /// <param name="property">属性定义节点</param>
        /// <param name="attribute">属性特性节点</param>
        /// <returns></returns>
        PropertyInfo CreatePropertyInfo(SemanticModel semanticModel, ClassDeclarationSyntax clazz, PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            var propertyName = property.Identifier.Text;
            var propertyType = semanticModel.GetTypeInfo(property.Type).Type;

            return new PropertyInfo
            {
                name = propertyName,
                type = propertyType.ToString(),
                isList = propertyType.IsObservableList(),
                location = property.GetLocation().ToLocationInfo(),
            };
        }

        /// <summary>
        /// 创建目标信息
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="clazz">类定义节点</param>
        /// <param name="property">属性定义节点</param>
        /// <param name="attribute">属性特性节点</param>
        /// <returns></returns>
        TargetInfo CreateTargetInfo(SemanticModel semanticModel, ClassDeclarationSyntax clazz, PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            //解析nameof 说明是绑定到某个element的某个属性
            var targetArgs = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression.ToString() == "nameof");
            var targetInvocationArgs = targetArgs == null ? null : targetArgs.Expression as InvocationExpressionSyntax;

            // 查找字符串常量/字面量作为元素路径
            string elementPath = FindStringConstantArgument(semanticModel, attribute);

            if (targetInvocationArgs == null || string.IsNullOrEmpty(elementPath))
            {
                return null;
            }

            string elementType = string.Empty;
            string targetPropertyName = string.Empty;
            string targetPropertyType = string.Empty;
            string targetPropertyValueType = string.Empty;

            var memberAccess = targetInvocationArgs.ArgumentList.Arguments[0].ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (memberAccess == null)
            {
                return null;
            }

            var sm = semanticModel.GetSymbolInfo(memberAccess.Expression);
            if (sm.Symbol is ITypeSymbol typeSymbol)
            {
                elementType = typeSymbol.ToString();
            }

            //如果这个属性是另一个类型的属性
            if (sm.Symbol is IPropertySymbol propertySymbol)
            {
                //targetPropertyName = propertySymbol.Name;
                //targetPropertyType = propertySymbol.Type.ToString();
            }

            var typeInfo = semanticModel.GetTypeInfo(memberAccess);
            targetPropertyName = memberAccess.Name.ToString();
            targetPropertyType = typeInfo.Type.ToString();
            foreach (var @interface in typeInfo.Type.AllInterfaces)
            {
                if (@interface.IsGenericType && @interface.ToString().StartsWith("FUI.Bindable.IBindableProperty"))
                {
                    targetPropertyValueType = @interface.TypeArguments[0].ToString();
                }
            }

            return new TargetInfo
            {
                type = elementType,
                path = elementPath,
                propertyType = targetPropertyType,
                propertyName = targetPropertyName,
                propertyValueType = targetPropertyValueType,
            };
        }

        /// <summary>
        /// 创建值转换器信息
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="clazz">类定义节点</param>
        /// <param name="property">属性定义节点</param>
        /// <param name="attribute">属性特性节点</param>
        /// <returns></returns>
        ConverterInfo CreateConverterInfo(SemanticModel semanticModel, ClassDeclarationSyntax clazz, PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            //当参数是类型时 说明转换器类型
            var args = attribute.ArgumentList.Arguments.FirstOrDefault((item) => item.Expression is TypeOfExpressionSyntax);

            if (args == null)
            {
                return null;
            }

            var typeArgs = args.Expression as TypeOfExpressionSyntax;
            var typeInfo = semanticModel.GetTypeInfo(typeArgs.Type);

            if (!typeInfo.Type.IsValueConverter(out var valueType, out var targetType))
            {
                return null;
            }

            return new ConverterInfo
            {
                type = typeInfo.Type.ToString(),
                sourceType = valueType.ToString(),
                targetType = targetType.ToString()
            };
        }

        /// <summary>
        /// 创建绑定模式信息
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="clazz">类定义节点</param>
        /// <param name="property">属性定义节点</param>
        /// <param name="attribute">属性特性节点</param>
        /// <returns></returns>
        BindingMode CreateBindingModeInfo(SemanticModel semanticModel, ClassDeclarationSyntax clazz, PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            //当参数是成员访问表达式时 说明是绑定类型
            var args = attribute.ArgumentList.Arguments.FirstOrDefault((item) => item.Expression is MemberAccessExpressionSyntax);
            if (args == null)
            {
                return BindingMode.OneWay;
            }

            var memberAccess = args.Expression as MemberAccessExpressionSyntax;
            return (BindingMode)Enum.Parse(typeof(BindingMode), memberAccess.Name.ToString());
        }

        /// <summary>
        /// 创建命令绑定配置文件
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="clazz">类定义文件</param>
        /// <param name="member">方法定义节点或事件字段节点</param>
        /// <param name="commandAttribute">命令声明特性节点</param>
        CommandBindingInfo CreateCommand(SemanticModel semanticModel, ClassDeclarationSyntax clazz, MemberDeclarationSyntax member, AttributeSyntax commandAttribute)
        {
            var methodName = string.Empty;
            var argsType = new List<string>();

            //获取方法命令绑定
            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                methodName = methodDeclaration.Identifier.Text;
                if (methodDeclaration.ParameterList != null)
                {
                    foreach (var parameter in methodDeclaration.ParameterList.Parameters)
                    {
                        var type = semanticModel.GetTypeInfo(parameter.Type);
                        argsType.Add(type.Type.ToString());
                    }
                }
            }

            //获取事件命令绑定
            if (member is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                methodName = Utility.GetEventMethodName(eventFieldDeclaration.Declaration.Variables.ToString());
                var type = eventFieldDeclaration.Declaration.Type;
                var typeInfo = semanticModel.GetTypeInfo(type);
                if (typeInfo.Type is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var arg in namedTypeSymbol.TypeArguments)
                    {
                        argsType.Add(arg.ToString());
                    }
                }
            }

            if (string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            var elementType = string.Empty;
            var elementPath = string.Empty;
            var targetPropertyName = string.Empty;

            // 仅解析第一个参数为元素路径（支持字符串字面量与常量）
            if (commandAttribute.ArgumentList != null && commandAttribute.ArgumentList.Arguments.Count > 0)
            {
                var firstArgExpr = commandAttribute.ArgumentList.Arguments[0].Expression;
                var constPath = GetStringConstant(semanticModel, firstArgExpr);
                if (!string.IsNullOrEmpty(constPath))
                {
                    elementPath = constPath;
                }
            }

            // 仅解析第二个参数为 nameof(Element.xxx)
            if (commandAttribute.ArgumentList != null && commandAttribute.ArgumentList.Arguments.Count > 1)
            {
                var secondArgExpr = commandAttribute.ArgumentList.Arguments[1].Expression as InvocationExpressionSyntax;
                if (secondArgExpr != null && secondArgExpr.Expression.ToString() == "nameof")
                {
                    var memberAccess = secondArgExpr.ArgumentList.Arguments[0].ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
                    if (memberAccess != null)
                    {
                        var sm = semanticModel.GetSymbolInfo(memberAccess.Expression);
                        if (sm.Symbol is ITypeSymbol typeSymbol)
                        {
                            elementType = typeSymbol.ToString();
                        }

                        // 如果这个属性是另一个类型的属性
                        if (sm.Symbol is IPropertySymbol)
                        {
                            //targetPropertyName = propertySymbol.Name;
                            //targetPropertyType = propertySymbol.Type.ToString();
                        }

                        targetPropertyName = memberAccess.Name.ToString();
                    }
                }
            }

            return new CommandBindingInfo
            {
                commandInfo = new CommandInfo
                {
                    isEvent = member is EventFieldDeclarationSyntax,
                    name = methodName,
                    parameters = argsType,
                    location = member.GetLocation().ToLocationInfo(),
                },

                targetInfo = new CommandTargetInfo
                {
                    path = elementPath,
                    type = elementType,
                    propertyName = targetPropertyName,
                    parameters = argsType,
                }
            };
        }

        /// <summary>
        /// 获取字符串常量值（支持字面量与 const 字段）
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="expression">表达式</param>
        /// <returns>常量字符串或 null</returns>
        string GetStringConstant(SemanticModel semanticModel, ExpressionSyntax expression)
        {
            if (expression == null)
            {
                return null;
            }

            if (expression is LiteralExpressionSyntax les && les.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return les.Token.ValueText;
            }

            var constValue = semanticModel.GetConstantValue(expression);
            if (constValue.HasValue && constValue.Value is string s)
            {
                return s;
            }

            return null;
        }

        /// <summary>
        /// 从特性参数中查找第一个字符串常量（字面量或 const）
        /// </summary>
        /// <param name="semanticModel">语义模型</param>
        /// <param name="attribute">特性</param>
        /// <returns>常量字符串或空</returns>
        string FindStringConstantArgument(SemanticModel semanticModel, AttributeSyntax attribute)
        {
            if (attribute == null || attribute.ArgumentList == null)
            {
                return null;
            }

            for (var i = 0; i < attribute.ArgumentList.Arguments.Count; i++)
            {
                var expr = attribute.ArgumentList.Arguments[i].Expression;
                var value = GetStringConstant(semanticModel, expr);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}