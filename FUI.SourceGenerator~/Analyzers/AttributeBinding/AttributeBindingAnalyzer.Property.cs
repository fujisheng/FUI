using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Linq;
using FUI.SourceGenerator;

namespace FUI.Analyzer.AttributeBinding
{
    public partial class AttributeBindingAnalyzer
    {
        #region Rules
        /// <summary>
        /// 绑定目标组件非FUI.IElement
        /// </summary>
        static readonly DiagnosticDescriptor TargetNotElementRule = Utility.CreateAttributeBindingRule(
            RuleIds.TargetNotElementRuleId,
            "Target '{0}' not 'FUI.IElement'");

        /// <summary>
        /// 绑定目标属性非 FUI.Bindable.BindableProperty
        /// </summary>
        static readonly DiagnosticDescriptor TargetPropertyNotBindableRule = Utility.CreateAttributeBindingRule(
            RuleIds.TargetPropertyNotBindableRuleId,
            "Target property '{0}' not 'FUI.Bindable.BindableProperty<>'.");

        /// <summary>
        /// 设置的转换器非 FUI.IValueConverter
        /// </summary>
        static readonly DiagnosticDescriptor ConverterNotIConverterRule = Utility.CreateAttributeBindingRule(
            RuleIds.ConverterNotIConverterRuleId,
            "Converter '{0}' not 'FUI.IValueConverter<,>'.");

        /// <summary>
        /// 没有转换器的时候 源属性无法转换成目标属性值类型
        /// </summary>
        static readonly DiagnosticDescriptor PropertyToTargetWithoutConverterRule = Utility.CreateAttributeBindingRule(
            RuleIds.PropertyToTargetWithoutConverterRuleId,
            "Can not convert property type '{0}' to target value type '{1}', Please consider using a 'ValueConverter' or changing the property type.");

        /// <summary>
        /// 有转换器的时候 源属性无法转换成转换器源类型 或 转换器目标类型无法转换成目标属性值类型
        /// </summary>
        static readonly DiagnosticDescriptor PropertyToTargetWithConverterRule = Utility.CreateAttributeBindingRule(
            RuleIds.PropertyToTargetWithConverterRuleId,
            "Can not convert property type '{0}' to converter source type '{1}' or converter target type '{2}' to target value type '{3}'.");

        /// <summary>
        /// 设置目标的时候必须通过nameof来赋值  以防止直接用字符串赋值而无法解析到对应的类型信息
        /// </summary>
        static readonly DiagnosticDescriptor TargetMustBeNameOfRule = Utility.CreateAttributeBindingRule(
            RuleIds.TargetMustBeNameOfRuleId,
            "The target must be assigned a value using 'nameof(Element.Property)'.");
        #endregion

        /// <summary>
        /// 属性绑定规则
        /// </summary>
        static readonly DiagnosticDescriptor[] PropertyBindingRules = new  DiagnosticDescriptor[]
        {
            TargetNotElementRule,
            TargetPropertyNotBindableRule,
            ConverterNotIConverterRule,
            PropertyToTargetWithoutConverterRule,
            PropertyToTargetWithConverterRule,
            TargetMustBeNameOfRule,
        };

        /// <summary>
        /// 分析一个属性的绑定标签是否合法
        /// </summary>
        /// <param name="context"></param>
        void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is PropertyDeclarationSyntax property))
            {
                return;
            }

            var attributes = property.AttributeLists.SelectMany((list) => list.Attributes);
            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetTypeInfo(attribute).Type.IsType(typeof(FUI.BindingAttribute)))
                {
                    AnalyzePropertyAttribute(context, property, attribute);
                }
            }
        }

        /// <summary>
        /// 分析属性的绑定标签是否合法
        /// </summary>
        void AnalyzePropertyAttribute(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            //解析Binding标签  判断是否合法
            var propertyType = context.SemanticModel.GetTypeInfo(property.Type).Type;
            var converterInfo = GetConverterType(context, attribute);
            var targetPropertyType = GetTargetPropertyType(context, attribute);

            //如果为空则返回
            if (propertyType == null || targetPropertyType == null)
            {
                return;
            }

            //如果属性和目标值类型都是可绑定列表，不进行下面的判断
            if (propertyType.IsObservableList() && targetPropertyType.IsObservableList())
            {
                return;
            }

            if (converterInfo == default)
            {
                //如果没有转换器且属性类型无法转换成目标值类型
                if (!propertyType.Extends(targetPropertyType))
                {
                    var diagnostic = Diagnostic.Create(PropertyToTargetWithoutConverterRule, attribute.GetLocation(), propertyType, targetPropertyType);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                //如果有转换器，但是无法将属性类型转换成转换器源类型，或无法将转换器目标类型转换成绑定目标值类型
                if (!propertyType.Extends(converterInfo.sourceType)
                    || !converterInfo.targetType.Extends(targetPropertyType))
                {
                    var diagnostic = Diagnostic.Create(PropertyToTargetWithConverterRule, attribute.GetLocation(), propertyType, converterInfo.sourceType, converterInfo.targetType, targetPropertyType);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 获取绑定目标属性的类型信息
        /// </summary>
        TypeInfo? GetTargetPropertyTypeInfo(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, out MemberAccessExpressionSyntax memberAccess)
        {
            memberAccess = null;

            //找到nameof
            var targetArgs = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression.ToString() == "nameof");

            //如果没有找到则报错
            if (targetArgs == null)
            {
                var diagnostic = Diagnostic.Create(TargetMustBeNameOfRule, attribute.GetLocation());
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            //找到nameof 里面的成员访问
            var targetInvocationArgs = targetArgs.Expression as InvocationExpressionSyntax;
            memberAccess = targetInvocationArgs.ArgumentList.Arguments[0].ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (memberAccess == null)
            {
                return null;
            }

            // 判断目标类型是否是继承自IElement
            var targetTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
            var elementTypeSymbol = targetTypeInfo.Type as INamedTypeSymbol;

            if (elementTypeSymbol == null || !elementTypeSymbol.Extends(typeof(FUI.IElement)))
            {
                var diagnostic = Diagnostic.Create(TargetNotElementRule, memberAccess.Expression.GetLocation(), elementTypeSymbol ?? (object)"<unknown>");
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            // 检查第一个参数（元素路径）允许为常量字符串（const 或 字面量）
            var elementPath = FUI.Analyzer.Utility.FindFirstStringConstantArgument(context.SemanticModel, attribute);
            if (string.IsNullOrEmpty(elementPath))
            {
                // 与生成器保持一致：如果没有常量字符串路径，则该绑定信息无效
                return null;
            }

            return context.SemanticModel.GetTypeInfo(memberAccess.Name);
        }

        /// <summary>
        /// 获取目标绑定属性的值类型
        /// </summary>
        INamedTypeSymbol GetTargetPropertyType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            //判断目标成员类型是否是BindableProperty
            var targetPropertyTypeInfo = GetTargetPropertyTypeInfo(context, attribute, out var memberAccess);
            if(targetPropertyTypeInfo == null)
            {
                return default;
            }

            //如果不是则报错
            if(!targetPropertyTypeInfo.Value.Type.IsBindableProperty(out var targetValueType))
            {
                var diagnostic = Diagnostic.Create(TargetPropertyNotBindableRule, memberAccess.Name.GetLocation(), targetPropertyTypeInfo.Value.Type);
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            //获取目标成员值类型
            return targetValueType as INamedTypeSymbol;
        }

        /// <summary>
        /// 获取绑定的转换器源类型和目标类型
        /// </summary>
        (INamedTypeSymbol sourceType, INamedTypeSymbol targetType) GetConverterType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            //找到typeof
            var converterTypeOf = attribute.ArgumentList.Arguments
                .FirstOrDefault((item) => item.Expression is TypeOfExpressionSyntax);

            if (converterTypeOf == null)
            {
                return default;
            }

            //找到对应的类型
            var typeofExpression = converterTypeOf.Expression as TypeOfExpressionSyntax;
            var typeInfo = context.SemanticModel.GetTypeInfo(typeofExpression.Type);

            //判断是否继承自IValueConverter<>
            if(!typeInfo.Type.IsValueConverter(out var sourceType, out var targetType))
            {
                var diagnostic = Diagnostic.Create(ConverterNotIConverterRule, typeofExpression.Type.GetLocation(), typeInfo.Type);
                context.ReportDiagnostic(diagnostic);
                return default;
            }

            //返回其sourcesType和targetType
            return (sourceType as INamedTypeSymbol, targetType as INamedTypeSymbol);
        }
    }
}
