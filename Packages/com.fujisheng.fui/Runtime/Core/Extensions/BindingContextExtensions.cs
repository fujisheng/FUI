using FUI.Bindable;
using FUI.Exceptions;

using System;

namespace FUI.Extensions
{
    public static class BindingContextExtensions
    {
        /// <summary>
        /// ��ViewModel�����Ե�View������
        /// </summary>
        /// <typeparam name="TProperty">��ӦViewModel��������</typeparam>
        /// <typeparam name="TElementPropertyValue">��ӦElement����ֵ����</typeparam>
        /// <param name="context">������λ</param>
        /// <param name="propertyChanged">��ӦViewModel���Ը���ί��</param>
        /// <param name="converter">ֵת����</param>
        /// <param name="elementProperty">��ӦElement������</param>
        /// <param name="propertyName">��ӦViewModel������  �����쳣</param>
        /// <param name="elementPath">��ӦElement·��  �����쳣</param>
        /// <param name="elementType">��ӦElement������  �����쳣</param>
        /// <param name="elementPropertyName">��ӦElement������  �����쳣</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Delegate BindindPropertyVM2V<TProperty, TElementPropertyValue>(
            this BindingContext context, 
            PropertyChangedHandler<TProperty> propertyChanged,
            IValueConverter converter,
            IWriteOnlyBindableProperty<TElementPropertyValue> elementProperty, 
            string propertyName,
            string elementPath,
            string elementType,
            string elementPropertyName)
        {
            PropertyChangedHandler<TProperty> valueChanged = (sender, oldValue, newValue) =>
            {
                if(converter == null)
                {
                    if(newValue is TElementPropertyValue elementPropertyValue)
                    {
                        elementProperty.SetValue(elementPropertyValue);
                    }
                    else
                    {
                        throw new BindingException(context.ViewModel, propertyName, context.View, elementPath, elementType, elementPropertyName, typeof(TElementPropertyValue), null);
                        throw new Exception($"can not convert {context.ViewModel}.{propertyName}({typeof(TProperty)}) to {context.View.Name}->{elementPath}({elementType}).{elementPropertyName}({typeof(TElementPropertyValue)})");
                    }
                }
                else if(converter is IForwardValueConverter<TProperty, TElementPropertyValue> valueConvertor)
                {
                    var convertedValue = valueConvertor.Convert(newValue);
                    elementProperty.SetValue(convertedValue);
                }
                else
                {
                    throw new Exception($"{converter} can not convert {context.ViewModel}.{propertyName}({typeof(TProperty)}) to {context.View.Name}->{elementPath}({elementType}).{elementPropertyName}({typeof(TElementPropertyValue)})");
                }
            };

            propertyChanged += valueChanged;
            return valueChanged;
        }
    }
}