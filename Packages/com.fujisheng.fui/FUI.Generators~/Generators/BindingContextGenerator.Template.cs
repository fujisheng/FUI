using System.Linq;

namespace FUI.Generators
{
    /// <summary>
    /// FUI数据绑定上下文生成器
    /// </summary>
    public partial class BindingContextGenerator
    {
        #region 类模板
        public static string BuildContextCode(
            BindingManifest contextInfo,
            string converters,
            string elementFields,
            string cacheElements,
            string externalSources,
            string clearElements,
            string bindings,
            string synchronizations,
            string unbindings,
            string functions)
        {
            var viewModelTypeName = contextInfo.viewModelType.GetTypeName(contextInfo.viewModelNamespace);
            var baseContextType = Utility.GetBindingContextBaseTypeName(contextInfo);
            var synchronizeBase = string.IsNullOrEmpty(contextInfo.baseViewModelType)
                ? string.Empty
                : "            base.OnSynchronize();";

            var classCode = $$"""
public partial class {{viewModelTypeName}}
{
    [FUI.Infrastructure.Binding.ViewModelAttribute(typeof({{contextInfo.viewModelType}}))]
    [FUI.Infrastructure.Binding.ViewAttribute("{{contextInfo.viewName}}")]
    public class {{viewModelTypeName}}_{{contextInfo.viewName}}_BindingContext : {{baseContextType}}
    {
{{converters}}
{{elementFields}}

        {{contextInfo.viewModelType}} vm => this.ViewModel as {{contextInfo.viewModelType}};

        public {{viewModelTypeName}}_{{contextInfo.viewName}}_BindingContext(FUI.Rendering.IView view, {{contextInfo.viewModelType}} viewModel) : base(view, viewModel) { }

        protected override void OnBinding()
        {
            base.OnBinding();
{{cacheElements}}
{{externalSources}}
{{bindings}}
        }

        protected override void OnSynchronize()
        {
{{synchronizeBase}}
{{synchronizations}}
        }

        protected override void OnUnbinding()
        {
{{unbindings}}
            if (this.View is FUI.Rendering.IExternalValueSynchronizationHost externalHost)
            {
                externalHost.ClearExternalValueSources();
            }
{{clearElements}}
            base.OnUnbinding();
        }

{{functions}}
    }
}
""";
            if (string.IsNullOrEmpty(contextInfo.viewModelNamespace))
            {
                return $$"""
{{Utility.FileHeader}}
{{classCode}}
""";
            }

            return $$"""
{{Utility.FileHeader}}
namespace {{contextInfo.viewModelNamespace}}
{
    {{classCode}}
}
""";
        }
        #endregion


        #region 属性绑定模板
        /// <summary>
        /// 构建属性绑定
        /// </summary>
        public static string BuildPropertyChangedFunctionCode(string functionName, string convert, string listBinding, PropertyBindingDescriptor info)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}(object sender, {{info.propertyInfo.type}} preValue, {{info.propertyInfo.type}} @value)
{
    {{convert}}
    var element = this.{{GetElementFieldName(info.targetInfo.path, info.targetInfo.type)}};
    if(element == null)
    {
        return;
    }
    {{listBinding}}
    element.{{info.targetInfo.propertyName}}?.SetValue(convertedValue);
}
""";
        }

        /// <summary>
        /// 构建List绑定
        /// </summary>
        public static string BuildListBindingCode(PropertyBindingDescriptor info)
        {
            return $$"""
FUI.Infrastructure.Binding.BindingContextUtility.BindingList<{{info.targetInfo.type}}>(element, preValue, @value);
""";
        }

        /// <summary>
        /// 构建List解绑方法
        /// </summary>
        /// <param name="functionName">方法名</param>
        /// <param name="info">属性绑定信息</param>
        /// <returns></returns>
        public static string BuildListUnbindingFunctionCode(string functionName, PropertyBindingDescriptor info)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    var element = this.{{GetElementFieldName(info.targetInfo.path, info.targetInfo.type)}};
    if(element == null)
    {
        return;
    }
    FUI.Infrastructure.Binding.BindingContextUtility.UnbindingList<{{info.targetInfo.type}}>(element, this.vm.{{info.propertyInfo.name}});
}
""";
        }

        /// <summary>
        /// 构建ListUnbinding
        /// </summary>
        public static string BuildListUnbindingCode(string functionName)
        {
            return $$"""
{{functionName}}();
""";
        }

        #endregion

        #region View到ViewModel绑定模板
        /// <summary>
        /// 构建View到ViewModel绑定方法
        /// </summary>
        public static string BuildV2VMBindingFunctionCode(BindingManifest contextInfo, PropertyBindingDescriptor info, string invocation, string functionName, string operate)
        {
            return $$"""
{{invocation}}
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    var element = this.{{GetElementFieldName(info.targetInfo.path, info.targetInfo.type)}};
    if(element == null)
    {
        return;
    }
    {{operate}}
}
""";
        }

        /// <summary>
        /// 构建View到ViewModel的值初始化方法
        /// </summary>
        /// <param name="contextInfo"></param>
        /// <param name="info"></param>
        /// <param name="functionName"></param>
        /// <returns></returns>
        public static string BuildV2VMInitFunctionCode(BindingManifest contextInfo, PropertyBindingDescriptor info, string functionName, string converters)
        {
            // 仅双向绑定保留非默认 VM 初值；只读视图源必须始终以实际元素为准。
            var preserveModel = info.bindingMode.HasFlag(BindingMode.OneWay) ? $$"""
    if(this.vm.{{info.propertyInfo.name}} != default)
    {
        return;
    }
""" : string.Empty;
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
{{preserveModel}}

    var element = this.{{GetElementFieldName(info.targetInfo.path, info.targetInfo.type)}};
    if(element == null)
    {
        return;
    }
    {{converters}}
    this.vm.{{info.propertyInfo.name}} = convertedValue;
}
""";
        }

        /// <summary>
        /// 构建View到ViewModel绑定执行的方法
        /// </summary>
        public static string BuildV2VMInvocationFunctionCode(BindingManifest contextInfo, PropertyBindingDescriptor info, string functionName, string converters)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}} ({{info.targetInfo.propertyValueType}} oldValue, {{info.targetInfo.propertyValueType}} newValue)
{
    {{converters}}
    this.vm.{{info.propertyInfo.name}} = convertedValue;
}
""";
        }

        /// <summary>
        /// 构建View到ViewModel绑定操作
        /// </summary>
        public static string BuildV2VMBindingOperateCode(string invocationName, PropertyBindingDescriptor info)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}.OnValueChanged += {{invocationName}};
""";
        }

        /// <summary>
        /// 构建View到ViewModel解绑操作
        /// </summary>
        public static string BuildV2VMUnbindingOperateCode(string invocationName, PropertyBindingDescriptor info)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}.OnValueChanged -= {{invocationName}};
""";
        }
        #endregion

        #region 命令绑定模板

        /// <summary>
        /// 构建命令绑定方法
        /// </summary>
        public static string BuildCommandBindingFunctionCode(BindingManifest contextInfo, CommandBindingDescriptor info, string functionName, string operate)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    var element = this.{{GetElementFieldName(info.targetInfo.path, info.targetInfo.type)}};
    if(element == null)
    {
        return;
    }
    {{operate}}
}
""";
        }

        /// <summary>
        /// 构建命令绑定操作
        /// </summary>
        public static string BuildCommandBindingOperateCode(BindingManifest contextInfo, CommandBindingDescriptor info, string listener)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}?.AddListener({{listener}});
""";
        }

        /// <summary>
        /// 构建命令解绑操作
        /// </summary>
        public static string BuildCommandUnbindingOperateCode(BindingManifest contextInfo, CommandBindingDescriptor info, string listener)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}?.RemoveListener({{listener}});
""";
        }

        public static string BuildAsyncCommandHandlerFunctionCode(CommandBindingDescriptor info, string functionName)
        {
            var parameters = string.Join(", ", info.commandInfo.parameters.Select((type, index) => $"{type} arg{index}"));
            var arguments = info.commandInfo.parameters.Select((_, index) => $"arg{index}").ToList();
            if (info.commandInfo.acceptsCancellationToken)
            {
                arguments.Add("token");
            }
            var invocation = $"this.vm.{info.commandInfo.name}({string.Join(", ", arguments)})";
            var operation = info.commandInfo.returnType == "System.Threading.Tasks.Task"
                ? $"new System.Threading.Tasks.ValueTask({invocation})"
                : invocation;
            return $$"""
void {{functionName}}({{parameters}})
{
    RunAsyncCommand(token => {{operation}});
}
""";
        }
        #endregion

        public static string GetPropertyTargetUniqueName(BindingManifest contextInfo, PropertyBindingDescriptor info)
        {
            return $"{info.propertyInfo.name}__{info.targetInfo.path.ToCSharpIdentifier()}_{info.targetInfo.type.ToCSharpIdentifier()}_{info.targetInfo.propertyName}";
        }

        public static string GetCommandTargetUniqueName(BindingManifest contextInfo, CommandBindingDescriptor info)
        {
            return $"{info.commandInfo.name}__{info.targetInfo.path.ToCSharpIdentifier()}_{info.targetInfo.type.ToCSharpIdentifier()}_{info.targetInfo.propertyName}";
        }

        public static string GetElementFieldName(string path, string type)
        {
            return $"element__{path.ToCSharpIdentifier()}__{type.ToCSharpIdentifier()}";
        }
    }
}
