using FUI.Bindable;
using FUI.Extensions;

using System;

namespace FUI.Exceptions
{
    public class BindingException : System.Exception
    {
        string message;
        public BindingException(ObservableObject viewModel, string propertyName, IView view, string elementPath, string elementType, string elementPropertyName, Type elementPropertyType, IValueConverter converter)
        {
            var propertyInfo = viewModel.GetType().GetProperty(propertyName);
            var propertyType = propertyInfo.PropertyType;

            if (converter == null)
            {
                message = $"can not convert {viewModel}.{propertyName}({propertyType}) to {view.Name}->{elementPath}({elementType}).{elementPropertyName}({elementPropertyType}), please consider using converter for this binding.";
            }
            else
            {
                message = $"{converter} can not convert {viewModel}.{propertyName}({propertyType}) to {view.Name}->{elementPath}({elementType}).{elementPropertyName}({elementPropertyType}), please consider using other converter for this binding.";
            }
        }

        public override string ToString()
        {
            return message;
        }

    }
}