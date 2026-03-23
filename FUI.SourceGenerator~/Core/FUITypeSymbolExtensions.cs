using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

namespace FUI.SourceGenerator
{
    internal static class FUITypeSymbolExtensions
    {
        /// <summary>
        /// 判断一个类型是否是可观察对象
        /// </summary>
        public static bool IsObservableObject(this ITypeSymbol type)
        {
            return type.Extends(typeof(FUI.Bindable.INotifyPropertyChanged));
        }

        /// <summary>
        /// 判断一个类型是否实现了ISynchronizeProperties接口
        /// </summary>
        public static bool IsISynchronizeProperties(this ITypeSymbol type)
        {
            return type.Extends(typeof(FUI.ISynchronizeProperties));
        }

        /// <summary>
        /// 判断一个类型是否是可观察列表
        /// </summary>
        public static bool IsObservableList(this ITypeSymbol type)
        {
            return type.Extends(typeof(FUI.Bindable.INotifyCollectionChanged));
        }

        /// <summary>
        /// 判断一个类型是否是可观察属性
        /// </summary>
        public static bool IsBindableProperty(this ITypeSymbol type, out ITypeSymbol propertyValueType)
        {
            propertyValueType = null;
            var bindablePropertyType = typeof(FUI.Bindable.IBindableProperty<>).GetGenericTypeDefinition();
            foreach (var interfaceType in type.AllInterfaces)
            {
                if (interfaceType.IsGenericType && interfaceType.ConstructedFrom.Matches(bindablePropertyType))
                {
                    propertyValueType = interfaceType.TypeArguments[0];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断一个类型是否是命令
        /// </summary>
        public static bool IsCommand(this ITypeSymbol type, out IReadOnlyList<ITypeSymbol> arguments)
        {
            var commandType = typeof(FUI.Bindable.CommandTemplate<>).GetGenericTypeDefinition();
            arguments = null;
            foreach (var t in type.GetBaseTypesAndThis())
            {
                if (t is not INamedTypeSymbol named)
                {
                    continue;
                }

                if (named.IsGenericType && named.ConstructedFrom.Matches(commandType))
                {
                    var args = named.TypeArguments;
                    if (args.Length != 1)
                    {
                        return false;
                    }

                    var actionType = args[0] as INamedTypeSymbol;
                    if (actionType == null)
                    {
                        return false;
                    }

                    arguments = actionType.TypeArguments;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断一个类型是否是值转换器
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="valueType">值转换器 值类型</param>
        /// <param name="targetType">值转换器 目标类型</param>
        /// <returns></returns>
        public static bool IsValueConverter(this ITypeSymbol type, out ITypeSymbol valueType, out ITypeSymbol targetType)
        {
            valueType = null;
            targetType = null;

            var valueConverterType = typeof(FUI.IValueConverter<,>).GetGenericTypeDefinition();

            foreach (var interfaceType in type.AllInterfaces)
            {
                if (interfaceType.IsGenericType && interfaceType.ConstructedFrom.Matches(valueConverterType))
                {
                    valueType = interfaceType.TypeArguments[0];
                    targetType = interfaceType.TypeArguments[1];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断一个类型是否是绑定上下文描述器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="viewModelType">viewModel类型</param>
        public static bool IsContextDescriptor(this ITypeSymbol type, out ITypeSymbol viewModelType)
        {
            viewModelType = null;
            var descriptorType = typeof(FUI.BindingDescriptor.ContextDescriptor<>).GetGenericTypeDefinition();
            foreach (var t in type.GetBaseTypesAndThis())
            {
                if (t is not INamedTypeSymbol named)
                {
                    continue;
                }

                if (named.IsGenericType && named.ConstructedFrom.Matches(descriptorType))
                {
                    viewModelType = named.TypeArguments[0];
                    return true;
                }
            }
            return false;
        }
    }
}
