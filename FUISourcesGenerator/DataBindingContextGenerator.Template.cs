namespace FUISourcesGenerator
{
    public partial class DataBindingContextGenerator
    {
        const string ViewNameMark = "*ViewName*";
        const string BindingMark = "*Binding*";
        const string UnbindingMark = "*Unbinding*";
        const string ViewModelTypeMark = "*ViewModelType*";
        const string ViewModelNameMark = "*ViewModelName*";
        const string ConvertersMark = "*Converters*";


        const string Template = @"
namespace __DataBindingGenerated
{
    public class __*ViewName*_Binding_Generated : FUI.UGUI.UGUIView
    {
*Converters*
        public __*ViewName*_Binding_Generated(FUI.ViewModel bindingContext, FUI.IAssetLoader assetLoader, string assetPath, string viewName) : base(bindingContext, assetLoader, assetPath, viewName) { }

        public __*ViewName*_Binding_Generated(FUI.ViewModel bindingContext, FUI.IAssetLoader assetLoader, UnityEngine.GameObject gameObject, string viewName) : base(bindingContext, assetLoader, gameObject, viewName) { }

        public override void Binding(FUI.Bindable.ObservableObject bindingContext)
        {
            base.Binding(bindingContext);
*Binding*
        }

        public override void Unbinding()
        {
            base.Unbinding();
*Unbinding*
        }

        protected override void OnPropertyChanged(object sender, string propertyName)
        {
        }
    }
}
";

        const string BindingItemsMark = "*BindingItems*";
        const string BindingTemplate = @"
            if(BindingContext is *ViewModelType* *ViewModelName*)
            {
*BindingItems*
                return;
            }
";
        const string PropertyChangedDelegateMark = "*PropertyChangedDelegate*";
        const string ConvertMark = "*Convert*";
        const string ElementTypeMark = "*ElementType*";
        const string ElementPathMark = "*ElementPath*";
        const string BindingItemTemplate = @"
*ViewModelName*.*PropertyChangedDelegate* += (sender, preValue, @value)=>
{
    *Convert*
    var element = GetVisualElement<*ElementType*>(""*ElementPath*"");
    if(element == null)
    {
        throw new System.Exception($""{this.Name} GetVisualElement type:{typeof(*ElementType*)} path:{@""*ElementPath*""} failed""); 
    }
    element.SetValue(convertedValue);
};
";
    }
}
