using FUI.Bindable;

namespace FUI.Extensions
{
    public static class ViewExtensions
    {
        /// <summary>
        /// 获取一个View的某个元素
        /// </summary>
        /// <typeparam name="TElement">元素类型</typeparam>
        /// <param name="view">对应的View</param>
        /// <param name="elementPath">元素路径</param>
        /// <returns>对应的元素</returns>
        /// <exception cref="System.Exception"></exception>
        public static TElement GetElement<TElement>(this IView view, string elementPath) where TElement : class, IElement
        {
            if (!(view is FUI.IElement e))
            {
                throw new System.Exception($"{view.Name} not FUI.IElement");
            }

            var element = e.GetChild<TElement>(elementPath);
            if (element == null)
            {
                throw new System.Exception($"{view.Name} not found {elementPath}:{typeof(TElement)}");
            }

            return element;
        }

        //public static void SetElementValue<TPropertyValueType, TValueType, TConverterValueType, TConverterTargetType>(IBindableProperty<TPropertyValueType> property, IValueConverter<TConverterValueType, TConverterTargetType> valueConverter, TValueType value)
        //{
        //    if (property == null)
        //    {
        //        return;
        //    }

        //    if (valueConverter != null)
        //    {
        //        var convertedValue = valueConverter.Convert(value);
        //        property.Value = convertedValue;
        //    }
        //    else
        //    {
        //        property.Value = (TPropertyValueType)(object)value;
        //    }
        //}
    }
}