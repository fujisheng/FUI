using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUI.Generators
{
    /// <summary>
    /// 绑定上下文生成器
    /// </summary>
    public partial class BindingContextGenerator
    {
        internal void Generate(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (typeSymbol == null || !typeSymbol.IsObservableObject())
            {
                return;
            }

            var contextInfoGenerator = new ContextInfoByAttributeGenerator();
            var bindingInfos = contextInfoGenerator.Generate(compilation, classDeclaration);
            foreach (var bindingInfo in bindingInfos)
            {
                GenerateBindingContextCode(context, bindingInfo);
            }
        }

        void GenerateBindingContextCode(SourceProductionContext context, BindingManifest bindingInfo)
        {
            var bindingBuilder = new StringBuilder();
            var synchronizationBuilder = new StringBuilder();
            var unbindingBuilder = new StringBuilder();
            HashSet<string> converterTypes = new HashSet<string>();

            var functionBuilder = new StringBuilder();
            var elementFieldsBuilder = new StringBuilder();
            var cacheElementsBuilder = new StringBuilder();
            var clearElementsBuilder = new StringBuilder();
            // 每个绑定目标在 Binding 时只查询一次，后续属性和 Command 回调直接访问强类型字段。
            var elementTargets = bindingInfo.properties
                .Select(property => (property.targetInfo.path, property.targetInfo.type))
                .Concat(bindingInfo.commands.Select(command => (command.targetInfo.path, command.targetInfo.type)))
                .Distinct()
                .ToArray();
            foreach (var target in elementTargets)
            {
                var fieldName = GetElementFieldName(target.path, target.type);
                elementFieldsBuilder.AppendLine($"        {target.type} {fieldName};");
                // 兼容 View 可以只提供自身需要的 Element 子集；完整性由默认 Route 的 Validator 检查。
                cacheElementsBuilder.AppendLine($"            this.{fieldName} = FUI.Rendering.ViewExtensions.TryGetElement<{target.type}>(this.View, @\"{target.path}\");");
                clearElementsBuilder.AppendLine($"            this.{fieldName} = null;");
            }

            // 只有反向绑定且无法事件通知的 Transform/RectTransform 属性才加入外部值同步。
            var externalFields = bindingInfo.properties
                .Where(property => property.bindingMode.HasFlag(BindingMode.OneWayToSource) && IsExternalValueProperty(property.targetInfo.propertyName))
                .Select(property => GetElementFieldName(property.targetInfo.path, property.targetInfo.type))
                .Distinct()
                .ToArray();
            var externalSources = string.Empty;
            if (externalFields.Length > 0)
            {
                var sources = string.Join(", ", externalFields.Select(field => $"this.{field} as FUI.Rendering.IExternalValueSource"));
                externalSources = $@"
            if (this.View is FUI.Rendering.IExternalValueSynchronizationHost externalHost)
            {{
                externalHost.SetExternalValueSources(new FUI.Rendering.IExternalValueSource[] {{ {sources} }});
            }}";
            }

            //为每个属性生成对应的委托 并添加绑定代码和解绑代码
            foreach (var property in bindingInfo.properties)
            {
                if (property.bindingMode.HasFlag(BindingMode.OneWay))
                {
                    BuildNormalPropertyBinding(bindingInfo, property, ref functionBuilder, ref bindingBuilder, ref unbindingBuilder);
                    var propertyTargetUniqueName = GetPropertyTargetUniqueName(bindingInfo, property);
                    synchronizationBuilder.AppendLine(
                        $"            PropertyChanged__{propertyTargetUniqueName}(this.vm, this.vm.{property.propertyInfo.name}, this.vm.{property.propertyInfo.name});");
                }

                //如果是列表 需要单独构建ListUnbinding代码
                if (property.propertyInfo.isList && property.bindingMode.HasFlag(BindingMode.OneWay))
                {
                    var listUnbindingFunctionName = $"UnbindingList_{GetPropertyTargetUniqueName(bindingInfo, property)}";
                    functionBuilder.AppendLine(BuildListUnbindingFunctionCode(listUnbindingFunctionName, property));
                    unbindingBuilder.AppendLine(BuildListUnbindingCode(listUnbindingFunctionName));
                }

                //如果是双向绑定需要构建从View到ViewModel的绑定
                if (property.bindingMode.HasFlag(BindingMode.OneWayToSource))
                {
                    BuildV2VMBinding(bindingInfo, property, ref bindingBuilder, ref unbindingBuilder, ref functionBuilder);
                    BuildV2VMInit(bindingInfo, property, ref bindingBuilder, ref functionBuilder);
                }

                //如果有转换器则需要记录以便生成转换器构造代码
                if (property.converterInfo != null)
                {
                    converterTypes.Add(property.converterInfo.type);
                }
            }

            //为命令生成绑定和解绑代码
            foreach (var command in bindingInfo.commands)
            {
                BuildCommandBinding(bindingInfo, command, ref bindingBuilder, ref unbindingBuilder, ref functionBuilder);
            }

            //生成所有的转换器构造代码
            var convertersBuilder = new StringBuilder();
            BuildConverterCostructor(converterTypes, ref convertersBuilder);


            //组装所有的绑定代码
            var code = BuildContextCode(
                bindingInfo,
                convertersBuilder.ToString(),
                elementFieldsBuilder.ToString(),
                cacheElementsBuilder.ToString(),
                externalSources,
                clearElementsBuilder.ToString(),
                bindingBuilder.ToString(),
                synchronizationBuilder.ToString(),
                unbindingBuilder.ToString(),
                functionBuilder.ToString());

            //格式化代码
            code = Utility.FormatCode(code);

            var viewNameFormat = string.IsNullOrEmpty(bindingInfo.viewName) ? "Empty" : bindingInfo.viewName;
            var fileName = $"{bindingInfo.viewModelType}.{viewNameFormat}.BindingContext.g";
            context.AddSource(fileName, code);
        }

        static bool IsExternalValueProperty(string propertyName)
        {
            return propertyName == "RotationValue"
                || propertyName == "WorldPositionValue"
                || propertyName == "LocalPositionValue"
                || propertyName == "LocalScaleValue"
                || propertyName == "AnchoredPositionValue"
                || propertyName == "LayoutValue"
                || propertyName == "Position"
                || propertyName == "LocalPosition"
                || propertyName == "Rotation"
                || propertyName == "LocalRotation"
                || propertyName == "LocalScale"
                || propertyName == "AnchorMin"
                || propertyName == "AnchorMax"
                || propertyName == "AnchoredPosition"
                || propertyName == "AnchoredPosition3D"
                || propertyName == "SizeDelta"
                || propertyName == "Pivot"
                || propertyName == "OffsetMin"
                || propertyName == "OffsetMax";
        }

        //构建普通属性绑定
        void BuildNormalPropertyBinding(BindingManifest bindingContext, PropertyBindingDescriptor property,
            ref StringBuilder functionBuilder,
            ref StringBuilder bindingItemsBuilder,
            ref StringBuilder unbindingItemsBuilder)
        {
            var delegateName = Utility.GetPropertyChangedDelegateName(property.propertyInfo.name);

            //构建值转换
            var convert = BuildConvert(bindingContext.viewModelType, property);

            //如果这个属性是List则生成List绑定代码
            var listBinding = string.Empty;
            if (property.propertyInfo.isList)
            {
                listBinding = BuildListBindingCode(property);
            }

            //为属性生成对应的绑定方法
            var propertyTargetUniqueName = GetPropertyTargetUniqueName(bindingContext, property);
            var propertyChangedFunctionName = $"PropertyChanged__{propertyTargetUniqueName}";

            //构建属性绑定代码
            functionBuilder.AppendLine(BuildPropertyChangedFunctionCode(propertyChangedFunctionName, convert, listBinding, property));

            //生成属性绑定代码
            bindingItemsBuilder.AppendLine($"this.vm.{delegateName} += {propertyChangedFunctionName};");

            //生成属性解绑代码
            unbindingItemsBuilder.AppendLine($"this.vm.{delegateName} -= {propertyChangedFunctionName};");
        }

        /// <summary>
        /// 构建值转换器
        /// </summary>
        string BuildConvert(string viewModelType, PropertyBindingDescriptor property)
        {
            var convertBuilder = new StringBuilder();

            if (property.converterInfo != null)
            {
                convertBuilder.AppendLine($"var convertedValue = {property.converterInfo.type.ToCSharpIdentifier()}.Convert(@value);");
            }
            else
            {
                convertBuilder.AppendLine($"var convertedValue = @value;");
            }
            return convertBuilder.ToString();
        }

        //构造所有的转换器构造代码
        void BuildConverterCostructor(HashSet<string> converterTypes, ref StringBuilder convertersBuilder)
        {
            foreach (var converterType in converterTypes)
            {
                if (string.IsNullOrEmpty(converterType))
                {
                    continue;
                }
                convertersBuilder.AppendLine($"{converterType} {converterType.ToCSharpIdentifier()} = new {converterType}();");
            }
        }

        /// <summary>
        /// 生成从View到ViewModel的绑定代码和解绑代码
        /// </summary>
        /// <param name="property">绑定配置</param>
        /// <param name="bindingItemBuilder">绑定代码构建器</param>
        /// <param name="unbindingItemBuilder">解绑代码构建器</param>
        /// <param name="functionBuilder">方法构建器</param>
        void BuildV2VMBinding(BindingManifest contextInfo, PropertyBindingDescriptor property,
            ref StringBuilder bindingItemBuilder,
            ref StringBuilder unbindingItemBuilder,
            ref StringBuilder functionBuilder)
        {
            var propertyTargetUniqueName = GetPropertyTargetUniqueName(contextInfo, property);
            var bindingFunctionName = $"BindingV2VM__{propertyTargetUniqueName}";
            var unbindingFunctionName = $"UnbindingV2VM__{propertyTargetUniqueName}";
            bindingItemBuilder.AppendLine($"{bindingFunctionName}();");
            unbindingItemBuilder.AppendLine($"{unbindingFunctionName}();");

            var invocationName = $"V2VMInvocation__{propertyTargetUniqueName}";

            var convert = BuildConvertBack(contextInfo.viewModelType, property, "@newValue");
            var invocation = BuildV2VMInvocationFunctionCode(contextInfo, property, invocationName, convert);

            var bindingOperate = BuildV2VMBindingOperateCode(invocationName, property);
            var unbindingOperate = BuildV2VMUnbindingOperateCode(invocationName, property);

            functionBuilder.AppendLine(BuildV2VMBindingFunctionCode(contextInfo, property, invocation, bindingFunctionName, bindingOperate));
            functionBuilder.AppendLine(BuildV2VMBindingFunctionCode(contextInfo, property, string.Empty, unbindingFunctionName, unbindingOperate));
        }

        /// <summary>
        /// 构建值转换器
        /// </summary>
        string BuildConvertBack(string viewModelType, PropertyBindingDescriptor property, string value)
        {
            var convertBuilder = new StringBuilder();

            if (property.converterInfo != null)
            {
                convertBuilder.AppendLine($"var convertedValue = {property.converterInfo.type.ToCSharpIdentifier()}.ConvertBack({value});");
            }
            else
            {
                convertBuilder.AppendLine($"var convertedValue = {value};");
            }
            return convertBuilder.ToString();
        }

        /// <summary>
        /// 构建从View到ViewModel的值初始化代码
        /// </summary>
        /// <param name="contextInfo">上下文信息</param>
        /// <param name="property">属性绑定信息</param>
        /// <param name="bindingItemBuilder">绑定方法builder</param>
        /// <param name="functionBuilder">方法builder</param>
        void BuildV2VMInit(BindingManifest contextInfo, PropertyBindingDescriptor property,
            ref StringBuilder bindingItemBuilder,
            ref StringBuilder functionBuilder)
        {
            var convertValue = $"element.{property.targetInfo.propertyName}.Value";
            var convert = BuildConvertBack(contextInfo.viewModelType, property, convertValue);
            var propertyTargetUniqueName = GetPropertyTargetUniqueName(contextInfo, property);
            var initFunctionName = $"InitV2VM__{propertyTargetUniqueName}";
            functionBuilder.AppendLine(BuildV2VMInitFunctionCode(contextInfo, property, initFunctionName, convert));
            bindingItemBuilder.AppendLine($"{initFunctionName}();");
        }

        /// <summary>
        /// 构建命令绑定和解绑代码
        /// </summary>
        /// <param name="command">绑定配置</param>
        /// <param name="bindingItemBuilder">绑定代码构建器</param>
        /// <param name="unbindingItemBuilder">解绑代码构建器</param>
        /// <param name="functionBuilder">方法构建器</param>
        void BuildCommandBinding(BindingManifest contextInfo, CommandBindingDescriptor command,
            ref StringBuilder bindingItemBuilder,
            ref StringBuilder unbindingItemBuilder,
            ref StringBuilder functionBuilder)
        {
            var commandTargetUniqueName = GetCommandTargetUniqueName(contextInfo, command);
            var bindingFunctionName = $"BindingCommand__{commandTargetUniqueName}";
            var unbindingFunctionName = $"UnbindingCommand__{commandTargetUniqueName}";
            var commandHandlerName = $"CommandHandler__{commandTargetUniqueName}";
            var isAsync = command.commandInfo.returnType == "System.Threading.Tasks.Task"
                || command.commandInfo.returnType == "System.Threading.Tasks.ValueTask";
            var listener = isAsync ? commandHandlerName : $"this.vm.{command.commandInfo.name}";

            bindingItemBuilder.AppendLine($"{bindingFunctionName}();");
            unbindingItemBuilder.AppendLine($"{unbindingFunctionName}();");

            var bindingOperate = BuildCommandBindingOperateCode(contextInfo, command, listener);
            var unbindingOperate = BuildCommandUnbindingOperateCode(contextInfo, command, listener);

            if (isAsync)
            {
                functionBuilder.AppendLine(BuildAsyncCommandHandlerFunctionCode(command, commandHandlerName));
            }

            functionBuilder.AppendLine(BuildCommandBindingFunctionCode(contextInfo, command, bindingFunctionName, bindingOperate));
            functionBuilder.AppendLine(BuildCommandBindingFunctionCode(contextInfo, command, unbindingFunctionName, unbindingOperate));
        }
    }
}
