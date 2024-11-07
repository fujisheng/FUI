using FUI.Bindable;
using FUI.Exceptions;

using System;

namespace FUI.Extensions
{
    public static class BindingContextExtensions
    {
        /// <summary>
        /// 绑定ViewModel的属性到View的属性
        /// </summary>
        /// <typeparam name="TProperty">对应ViewModel属性类型</typeparam>
        /// <typeparam name="TElementPropertyValue">对应Element属性值类型</typeparam>
        /// <param name="context">绑定上下位</param>
        /// <param name="propertyChanged">对应ViewModel属性更改委托</param>
        /// <param name="converter">值转换器</param>
        /// <param name="elementProperty">对应Element的属性</param>
        /// <param name="propertyName">对应ViewModel属性名  用于异常</param>
        /// <param name="elementPath">对应Element路径  用于异常</param>
        /// <param name="elementType">对应Element的类型  用于异常</param>
        /// <param name="elementPropertyName">对应Element属性名  用于异常</param>
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