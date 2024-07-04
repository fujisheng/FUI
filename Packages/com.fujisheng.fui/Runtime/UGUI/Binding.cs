using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
    [Serializable]
    public class TypeInfo
    {
        public string name;
        public string fullName;
        public bool isGenericType;
        public bool isValueType;
        public List<TypeInfo> genericArguments = new List<TypeInfo>();

        public static TypeInfo Create(Type type)
        {
            if(type == null)
            {
                return null;
            }

            var typeInfo = new TypeInfo();
            typeInfo.name = type.Name;
            typeInfo.fullName = type.FullName;
            typeInfo.isGenericType = type.IsGenericType;
            typeInfo.isValueType = type.IsValueType;
            if (type.IsGenericType)
            {
                foreach (var argType in type.GetGenericArguments())
                {
                    typeInfo.genericArguments.Add(Create(argType));
                }
            }
            return typeInfo;
        }

        public string ToTypeString()
        {
            if (isGenericType)
            {
                var preName = fullName.Substring(0, fullName.IndexOf("`"));
                return $"{preName}<{string.Join(",", genericArguments.ConvertAll(arg => arg.ToTypeString()).ToArray())}>";
            }
            return fullName;
        }

        public bool IsNull()
        {
            return string.IsNullOrEmpty(name) || string.IsNullOrEmpty(fullName);
        }

        public override string ToString()
        {
            return ToTypeString();
        }
    }

    [Serializable]
    public class BindingProperty
    {
        public string name;
        [SerializeField]
        public TypeInfo type;
        [SerializeField]
        public TypeInfo converterType;
        public string elementPath;
        [SerializeField]
        public TypeInfo elementType;

        [SerializeField]
        public TypeInfo converterValueType;
        [SerializeField]
        public TypeInfo converterTargetType;
        [SerializeField]
        public TypeInfo elementValueType;
    }

    [Serializable]
    public class BindingContext
    {
        public string type = typeof(ViewModel).FullName;
        [SerializeField]
        public List<BindingProperty> properties = new List<BindingProperty>();
    }

    [Serializable]
    public class BindingConfig
    {
        public string viewName = "TestView";
        [SerializeField]
        public List<BindingContext> contexts = new List<BindingContext>();
    }

    public class Binding : MonoBehaviour
    {
        [SerializeField]
        public BindingConfig config = new BindingConfig();
    }
}