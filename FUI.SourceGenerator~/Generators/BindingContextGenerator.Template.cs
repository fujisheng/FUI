namespace FUI.SourceGenerator.Generators
{
    /// <summary>
    /// FUI数据绑定上下文生成器
    /// </summary>
    public partial class BindingContextGenerator
    {
        #region 类模板
        public static string BuildContextCode(ContextBindingInfo contextInfo, string converters, string bindings, string unbindings, string functions)
        {
            var viewModelTypeName = contextInfo.viewModelType.GetTypeName(contextInfo.viewModelNamespace);
            var baseContextType = Utility.GetBindingContextBaseTypeName(contextInfo);

            var classCode = $$"""
public partial class {{viewModelTypeName}}
{
    [FUI.ViewModelAttribute(typeof({{contextInfo.viewModelType}}))]
    [FUI.ViewAttribute("{{contextInfo.viewName}}")]
    public class {{viewModelTypeName}}_{{contextInfo.viewName}}_BindingContext : {{baseContextType}}
    {
{{converters}}

        {{contextInfo.viewModelType}} vm => this.ViewModel as {{contextInfo.viewModelType}};

        public {{viewModelTypeName}}_{{contextInfo.viewName}}_BindingContext(FUI.IView view, {{contextInfo.viewModelType}} viewModel) : base(view, viewModel) { }

        protected override void OnBinding()
        {
            base.OnBinding();
{{bindings}}
        }

        protected override void OnUnbinding()
        {
            base.OnUnbinding();
{{unbindings}}
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
        public static string BuildPropertyChangedFunctionCode(string functionName, string convert, string listBinding, PropertyBindingInfo info)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}(object sender, {{info.propertyInfo.type}} preValue, {{info.propertyInfo.type}} @value)
{
    {{convert}}
    var element = FUI.Extensions.ViewExtensions.GetElement<{{info.targetInfo.type}}>(this.View, @"{{info.targetInfo.path}}");
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
        public static string BuildListBindingCode(PropertyBindingInfo info)
        {
            return $$"""
FUI.BindingContextUtility.BindingList<{{info.targetInfo.type}}>(element, preValue, @value);
""";
        }

        /// <summary>
        /// 构建List解绑方法
        /// </summary>
        /// <param name="functionName">方法名</param>
        /// <param name="info">属性绑定信息</param>
        /// <returns></returns>
        public static string BuildListUnbindingFunctionCode(string functionName, PropertyBindingInfo info)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    var element = FUI.Extensions.ViewExtensions.GetElement<{{info.targetInfo.type}}>(this.View, @"{{info.targetInfo.path}}");
    if(element == null)
    {
        return;
    }
    FUI.BindingContextUtility.UnbindingList<{{info.targetInfo.type}}>(element, this.vm.{{info.propertyInfo.name}});
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
        public static string BuildV2VMBindingFunctionCode(ContextBindingInfo contextInfo, PropertyBindingInfo info, string invocation, string functionName, string operate)
        {
            return $$"""
{{invocation}}
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    var element = FUI.Extensions.ViewExtensions.GetElement<{{info.targetInfo.type}}>(this.View, @"{{info.targetInfo.path}}");
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
        public static string BuildV2VMInitFunctionCode(ContextBindingInfo contextInfo, PropertyBindingInfo info, string functionName, string converters)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    if(this.vm.{{info.propertyInfo.name}} != default)
    {
        return;
    }

    var element = FUI.Extensions.ViewExtensions.GetElement<{{info.targetInfo.type}}>(this.View, @"{{info.targetInfo.path}}");
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
        public static string BuildV2VMInvocationFunctionCode(ContextBindingInfo contextInfo, PropertyBindingInfo info, string functionName, string converters)
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
        public static string BuildV2VMBindingOperateCode(string invocationName, PropertyBindingInfo info)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}.OnValueChanged += {{invocationName}};
""";
        }

        /// <summary>
        /// 构建View到ViewModel解绑操作
        /// </summary>
        public static string BuildV2VMUnbindingOperateCode(string invocationName, PropertyBindingInfo info)
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
        public static string BuildCommandBindingFunctionCode(ContextBindingInfo contextInfo, CommandBindingInfo info, string functionName, string operate)
        {
            return $$"""
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
void {{functionName}}()
{
    var element = FUI.Extensions.ViewExtensions.GetElement<{{info.targetInfo.type}}>(this.View, "{{info.targetInfo.path}}");
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
        public static string BuildCommandBindingOperateCode(ContextBindingInfo contextInfo, CommandBindingInfo info)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}?.AddListener(this.vm.{{info.commandInfo.name}});
""";
        }

        /// <summary>
        /// 构建命令解绑操作
        /// </summary>
        public static string BuildCommandUnbindingOperateCode(ContextBindingInfo contextInfo, CommandBindingInfo info)
        {
            return $$"""
element.{{info.targetInfo.propertyName}}?.RemoveListener(this.vm.{{info.commandInfo.name}});
""";
        }
        #endregion

        public static string GetPropertyTargetUniqueName(ContextBindingInfo contextInfo, PropertyBindingInfo info)
        {
            return $"{info.propertyInfo.name}__{info.targetInfo.path.ToCSharpIdentifier()}_{info.targetInfo.type.ToCSharpIdentifier()}_{info.targetInfo.propertyName}";
        }

        public static string GetCommandTargetUniqueName(ContextBindingInfo contextInfo, CommandBindingInfo info)
        {
            return $"{info.commandInfo.name}__{info.targetInfo.path.ToCSharpIdentifier()}_{info.targetInfo.type.ToCSharpIdentifier()}_{info.targetInfo.propertyName}";
        }
    }
}
