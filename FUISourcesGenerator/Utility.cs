using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Mono.Cecil;

namespace FUISourcesGenerator
{
    public static class Utility
    {
        public static string ObservableObjectFullName = "FUI.ObservableObjectAttribute";
        public static string ObservablePropertyFullName = "FUI.ObservablePropertyAttribute";
        public static string SynchronizePropertiesFullName = "FUI.ISynchronizeProperties";
        public static string SynchronizePropertiesMethodName = "Synchronize";

        /// <summary>
        /// 是否拥有某个自定义特性
        /// </summary>
        /// <param name="target"></param>
        /// <param name="attributeFullName">特性类型全名</param>
        /// <returns></returns>
        public static bool HasCustomAttribute(this ICustomAttributeProvider target, string attributeFullName)
        {
            foreach (var attribute in target.CustomAttributes)
            {
                if (attribute.AttributeType.FullName == attributeFullName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取一个泛型格式化后的名字
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetGenericTypeName(string type)
        {
            var tIndex = type.IndexOf("`");
            if (tIndex < 0)
            {
                return type;
            }

            var pre = type.Substring(0, tIndex);
            var leftIndex = type.IndexOf("<");
            var rightIndex = type.IndexOf(">");
            var genericTypes = type.Substring(leftIndex + 1, rightIndex - leftIndex - 1);
            var genericTypeNames = genericTypes.Split(',');
            var argsType = "<";
            for (int i = 0; i < genericTypeNames.Length; i++)
            {
                argsType += i == genericTypeNames.Length - 1 ? genericTypeNames[i] : $"{genericTypeNames[i]},";
            }
            argsType += ">";
            return $"{pre}{argsType}";
        }

        /// <summary>
        /// 将一个类型名转换成合法的命名
        /// </summary>
        public static string TypeToCSharpName(string type)
        {
            return type.Replace("<", "_").Replace(">", "_").Replace(".", "_");
        }

        /// <summary>
        /// 格式化代码
        /// </summary>
        /// <param name="text">代码</param>
        /// <returns></returns>
        public static string NormalizeCode(string text)
        {
            return CSharpSyntaxTree.ParseText(text).GetRoot().NormalizeWhitespace().ToFullString();
        } 

        /// <summary>
        /// 获取属性更改委托名字
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string GetPropertyChangedDelegateName(string propertyName)
        {
            return $"_{propertyName}_Changed";
        }

        /// <summary>
        /// 是否有ObserableObject特性
        /// </summary>
        /// <param name="classDeclaration"></param>
        /// <returns></returns>
        public static bool HasObservableObjectAttribute(ClassDeclarationSyntax classDeclaration)
        {
            var classAttributes = classDeclaration.AttributeLists.ToList();
            foreach (var att in classAttributes)
            {
                foreach (var node in att.ChildNodes().OfType<AttributeSyntax>())
                {
                    foreach (var id in node.ChildNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (id.ToString() == "ObservableObject"
                            || id.ToString() == "ObservableObjectAttribute"
                            || id.ToString() == "FUI.Bindable.ObservableObjectAttribute"
                            || id.ToString() == "Bindable.ObservableObjectAttribute"
                            || id.ToString() == "FUI.Bindable.ObservableObject"
                            || id.ToString() == "Bindable.ObservableObject")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 是否是视图模型
        /// </summary>
        public static bool IsViewModel(ClassDeclarationSyntax classDeclaration)
        {
            if(classDeclaration.BaseList == null)
            {
                return false;
            }

            foreach(var baseType in classDeclaration.BaseList.Types)
            {
                if(baseType.Type.ToString() == "ViewModel")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否是可观察对象
        /// </summary>
        public static bool IsObservableObject(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.BaseList == null)
            {
                return false;
            }
            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                if (baseType.Type.ToString() == "ObservableObject")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 是否有ObservableProperty特性
        /// </summary>
        /// <param name="propertyDeclaration"></param>
        /// <returns></returns>
        public static bool HasObservablePropertyAttribute(PropertyDeclarationSyntax propertyDeclaration)
        {
            var propertyAttributes = propertyDeclaration.AttributeLists.ToList();
            foreach (var att in propertyAttributes)
            {
                foreach (var node in att.ChildNodes().OfType<AttributeSyntax>())
                {
                    foreach (var id in node.ChildNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (id.ToString() == "ObservableProperty"
                           || id.ToString() == "ObservablePropertyAttribute"
                           || id.ToString() == "FUI.Bindable.ObservablePropertyAttribute"
                           || id.ToString() == "Bindable.ObservablePropertyAttribute"
                           || id.ToString() == "FUI.Bindable.ObservableProperty"
                           || id.ToString() == "Bindable.ObservableProperty")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
