using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace FUI.SourceGenerator
{
    internal static class TypeSymbolExtensions
    {
        static Queue<ITypeSymbol> cache = new Queue<ITypeSymbol>();

        /// <summary>
        /// 判断一个类型是否继承自某个类型或者实现了某个接口
        /// </summary>
        /// <param name="self">源类型</param>
        /// <param name="other">目标类型</param>
        /// <returns></returns>
        internal static bool Extends(this ITypeSymbol self, Type other) => Extends(self, other, (s, t) => s.Matches(t));

        /// <summary>
        /// 判断一个类型是否继承自另一个类型或者实现了某个接口
        /// </summary>
        /// <param name="symbol">源类型</param>
        /// <param name="other">目标类型</param>
        /// <returns></returns>
        internal static bool Extends(this ITypeSymbol symbol, ITypeSymbol other) => Extends(symbol, other, (s, t) => s.Matches(t));

        /// <summary>
        /// 判断一个类型是否继承自另一个类型或者实现了某个接口
        /// </summary>
        static bool Extends<T>(this ITypeSymbol self, T other, Func<ITypeSymbol, T, bool> matches)
        {
            if (self == null || other == null)
            {
                return false;
            }

            var openList = cache;
            openList.Clear();
            openList.Enqueue(self);

            while (openList.Count > 0)
            {
                var current = openList.Dequeue();

                if (matches.Invoke(current, other))
                {
                    return true;
                }

                if (current.BaseType != null)
                {
                    openList.Enqueue(current.BaseType);
                }

                foreach (var @interface in current.Interfaces)
                {
                    openList.Enqueue(@interface);
                }
            }

            return false;
        }


        /// <summary>
        /// 判断一个类型是否和另一个类型匹配 
        /// </summary>
        /// <param name="symbol">源类型</param>
        /// <param name="type">目标类型</param>
        /// <returns></returns>
        internal static bool Matches(this ITypeSymbol symbol, Type type)
        {
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Void:
                    return type == typeof(void);
                case SpecialType.System_Boolean:
                    return type == typeof(bool);
                case SpecialType.System_Int32:
                    return type == typeof(int);
                case SpecialType.System_Single:
                    return type == typeof(float);
            }

            if (type.IsArray)
            {
                return symbol is IArrayTypeSymbol array && Matches(array.ElementType, type.GetElementType());
            }

            if (!(symbol is INamedTypeSymbol named))
            {
                return false;
            }

            if (type.IsConstructedGenericType)
            {
                var args = type.GetTypeInfo().GenericTypeArguments;
                if (args.Length != named.TypeArguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (!Matches(named.TypeArguments[i], args[i]))
                    {
                        return false;
                    }
                }

                return Matches(named.ConstructedFrom, type.GetGenericTypeDefinition());
            }

            return named.MetadataName == type.Name && named.ContainingNamespace?.ToDisplayString() == type.Namespace;
        }

        /// <summary>
        /// 判断一个类型是否和另一个类型匹配 
        /// </summary>
        /// <param name="self">源类型</param>
        /// <param name="other">目标类型</param>
        /// <returns></returns>
        internal static bool Matches(this ITypeSymbol self, ITypeSymbol other)
        {
            switch (self.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Int32:
                case SpecialType.System_Single:
                    return self.SpecialType == other.SpecialType;
            }

            if (other is IArrayTypeSymbol otherArray)
            {
                return self is IArrayTypeSymbol array && Matches(array.ElementType, otherArray);
            }

            if (!(self is INamedTypeSymbol selfNamed) || !(other is INamedTypeSymbol otherNamed))
            {
                return false;
            }

            if (selfNamed.IsGenericType && otherNamed.IsGenericType)
            {
                var otherTypeArgs = otherNamed.TypeArguments;
                if (otherTypeArgs.Length != selfNamed.TypeArguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < otherTypeArgs.Length; i++)
                {
                    if (!Matches(selfNamed.TypeArguments[i], otherTypeArgs[i]))
                    {
                        return false;
                    }
                }

                return Matches(selfNamed.ConstructedFrom, otherNamed.ConstructedFrom);
            }

            return selfNamed.MetadataName == other.MetadataName && selfNamed.ContainingNamespace?.ToDisplayString() == other.ContainingNamespace?.ToDisplayString();
        }

        /// <summary>
        /// 判断某个类型是否是指定类型
        /// </summary>
        /// <param name="symbol">要判断的类型</param>
        /// <param name="type">目标类型</param>
        /// <returns></returns>
        internal static bool IsType(this ITypeSymbol symbol, Type type)
        {
            return Matches(symbol, type);
        }

        /// <summary>
        /// 获取类型的所有基类和自身
        /// </summary>
        internal static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
