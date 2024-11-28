using System;
using System.Collections.Generic;

namespace FUI.Editor
{
    public static class TypeExtensions
    {
        static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        static TypeExtensions()
        {
            CompilerEditor.OnCompilationComplete += () =>
            {
                typeCache.Clear();
            };
        }

        public static Type GetNamedType(this string typeFullName)
        {
            if (typeCache.TryGetValue(typeFullName, out var type))
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeFullName);
                if (type != null)
                {
                    typeCache[typeFullName] = type;
                    return type;
                }
            }

            return null;
        }

        public static string GetTypeName(this string typeFullName)
        {
            var type = typeFullName.GetNamedType();
            if (type == null)
            {
                return typeFullName;
            }

            return type.Name;
        }
    }
}