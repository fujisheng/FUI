using FUI.Bindable;

namespace FUI.Extensions
{
    public static class ViewExtensions
    {
        /// <summary>
        /// ��ȡһ��View��ĳ��Ԫ��
        /// </summary>
        /// <typeparam name="TElement">Ԫ������</typeparam>
        /// <param name="view">��Ӧ��View</param>
        /// <param name="elementPath">Ԫ��·��</param>
        /// <returns>��Ӧ��Ԫ��</returns>
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