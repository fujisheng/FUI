using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Mono.Cecil;

namespace FUICompiler
{
    public static class Utility
    {
        public static string ObservableObjectFullName = "FUI.ObservableObjectAttribute";
        public static string ObservablePropertyFullName = "FUI.ObservablePropertyAttribute";
        public static string SynchronizePropertiesFullName = "FUI.ISynchronizeProperties";
        public static string SynchronizePropertiesMethodName = "Synchronize";
        public static string BindingConfigFullName = "FUI.BindingAttribute";

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
        /// 获取一个自定义特性
        /// </summary>
        /// <param name="target"></param>
        /// <param name="attributeFullName"></param>
        /// <returns></returns>
        public static TypeReference GetCustomAttribute(this ICustomAttributeProvider target, string attributeFullName)
        {
            foreach (var attribute in target.CustomAttributes)
            {
                if (attribute.AttributeType.FullName == attributeFullName)
                {
                    return attribute.AttributeType;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取一个泛型格式化后的名字
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static string ToGenericTypeName(this string typeName)
        {
            var tIndex = typeName.IndexOf("`");
            if (tIndex < 0)
            {
                return typeName;
            }

            var pre = typeName.Substring(0, tIndex);
            var leftIndex = typeName.IndexOf("<");
            var rightIndex = typeName.IndexOf(">");
            var genericTypes = typeName.Substring(leftIndex + 1, rightIndex - leftIndex - 1);
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
        /// 可观察对象基类类型名
        /// </summary>
        static string[] ObservableObjectBaseTypes = new string[]
        {
            "ObservableObject",
            "Bindable.ObservableObject",
            "FUI.Bindable.ObservableObject",
            "ViewModel",
            "FUI.ViewModel",
        };

        /// <summary>
        /// 可观察对象特性名
        /// </summary>
        static string[] ObservableObjectAttributes = new string[]
        {
            "ObservableObject",
            "FUI.ObservableObject",
            "ObservableObjectAttribute",
            "FUI.ObservableObjectAttribute",

            "Binding",
            "BindingAttribute",
            "FUI.Binding",
            "FUI.BindingAttribute"
        };
        
        /// <summary>
        /// 是否是可观察对象
        /// </summary>
        /// <param name="classDeclaration">类型定义</param>
        /// <returns></returns>
        public static bool IsObservableObject(ClassDeclarationSyntax classDeclaration)
        {
            //基类不为空
            if(classDeclaration.BaseList != null)
            {
                foreach(var baseType in classDeclaration.BaseList.Types)
                {
                    if(ObservableObjectBaseTypes.Contains(baseType.Type.ToString()))
                    {
                        return true;
                    }
                }
            };
            
            //特性不为空
            if(classDeclaration.AttributeLists != null)
            {
                foreach(var att in classDeclaration.AttributeLists)
                {
                    foreach(var node in att.ChildNodes().OfType<AttributeSyntax>())
                    {
                        foreach(var id in node.ChildNodes().OfType<IdentifierNameSyntax>())
                        {
                            if(ObservableObjectAttributes.Contains(id.ToString()))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 可绑定属性特性名
        /// </summary>
        static string[] ObservablePropertyAttributes = new string[]
        {
            "ObservableProperty",
            "ObservablePropertyAttribute",
            "FUI.ObservableProperty",
            "FUI.ObservablePropertyAttribute",
            
            "Binding",
            "BindingAttribute",
            "FUI.Binding",
            "FUI.BindingAttribute"
        };

        /// <summary>
        /// 可绑定属性忽略特性名
        /// </summary>
        static string[] ObservablePropertyIgnoreAttributes = new string[]
        {
            "ObservablePropertyIgnore",
            "ObservablePropertyIgnoreAttribute",
            "FUI.ObservablePropertyIgnore",
            "FUI.ObservablePropertyIgnoreAttribute"
        };

        /// <summary>
        /// 判断是一个属性是否是可观察属性
        /// </summary>
        /// <param name="clazz"></param>
        /// <param name="propertyDeclaration"></param>
        /// <returns></returns>
        public static bool IsObservableProperty(ClassDeclarationSyntax clazz, PropertyDeclarationSyntax propertyDeclaration)
        {
            if (!IsObservableObject(clazz))
            {
                return false;
            }

            var propertyAttributes = propertyDeclaration.AttributeLists.ToList();
            foreach (var att in propertyAttributes)
            {
                foreach (var node in att.ChildNodes().OfType<AttributeSyntax>())
                {
                    foreach (var id in node.ChildNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (ObservablePropertyIgnoreAttributes.Contains(id.ToString()))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 判断一个类型定义是否为可观察对象
        /// </summary>
        /// <param name="type">类型定义</param>
        /// <returns></returns>
        public static bool IsObservableObject(TypeDefinition type)
        {
            if(type.CustomAttributes == null)
            {
                return false;
            }

            foreach(var attribute in type.CustomAttributes)
            {
                if(ObservableObjectAttributes.Contains(attribute.AttributeType.FullName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断一个属性是否为可观察属性
        /// </summary>
        /// <param name="property">属性定义</param>
        /// <returns></returns>
        public static bool IsObservableProperty(PropertyDefinition property)
        {
            if (!IsObservableObject(property.DeclaringType))
            {
                return false;
            }

            if(property.CustomAttributes != null)
            {
                foreach (var attribute in property.CustomAttributes)
                {
                    if (ObservablePropertyIgnoreAttributes.Contains(attribute.AttributeType.FullName))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 通过特性绑定的特性名
        /// </summary>
        static string[] BindingAttributes = new string[]
        {
            "Binding",
            "BindingAttribute",
            "FUI.Binding",
            "FUI.BindingAttribute"
        };

        /// <summary>
        /// 尝试获取绑定特性
        /// </summary>
        /// <param name="classDeclaration">类型定义文件</param>
        /// <param name="attributes">特性</param>
        /// <returns></returns>
        public static bool TryGetClassBindingAttribute(ClassDeclarationSyntax classDeclaration, out List<AttributeSyntax> attributes)
        {
            attributes = new List<AttributeSyntax>();
            var classAttributes = classDeclaration.AttributeLists.ToList();
            foreach (var att in classAttributes)
            {
                foreach (var node in att.ChildNodes().OfType<AttributeSyntax>())
                {
                    foreach (var id in node.ChildNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (BindingAttributes.Contains(id.ToString()))
                        {
                            attributes.Add(node);
                        }
                    }
                }
            }
            return attributes.Count > 0;
        }

        /// <summary>
        /// 尝试获取属性绑定特性
        /// </summary>
        /// <param name="propertyDeclaration"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static bool TryGetPropertyBindingAttribute(PropertyDeclarationSyntax propertyDeclaration, out List<AttributeSyntax> attributes)
        {
            attributes = new List<AttributeSyntax>();
            var propertyAttributes = propertyDeclaration.AttributeLists.ToList();
            foreach (var att in propertyAttributes)
            {
                foreach (var node in att.ChildNodes().OfType<AttributeSyntax>())
                {
                    foreach (var id in node.ChildNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (BindingAttributes.Contains(id.ToString()))
                        {
                            attributes.Add(node);
                        }
                    }
                }
            }
            return attributes.Count > 0;
        }

        /// <summary>
        /// 尝试获取命名空间
        /// </summary>
        /// <param name="classDeclaration">类定义</param>
        /// <param name="namespaceName">命名空间名</param>
        /// <returns></returns>
        public static bool TryGetNamespace(ClassDeclarationSyntax classDeclaration, out string namespaceName)
        {
            namespaceName = null;
            var parent = classDeclaration.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    namespaceName = namespaceDeclaration.Name.ToString();
                    return true;
                }
                parent = parent.Parent;
            }
            return false;
        }

        /// <summary>
        /// 获取或者创建一个文件夹
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static DirectoryInfo GetOrCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return new DirectoryInfo(path);
            }

            return Directory.CreateDirectory(path);
        }

        public static bool IsNull(this TypeInfo typeInfo)
        {
            return typeInfo == null || typeInfo.IsNull();
        }
    }
}
