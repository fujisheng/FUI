using Mono.Cecil;

namespace FUISourcesGenerator
{
    public static class InjecterExtensions
    {
        /// <summary>
        /// 获取一个属性的定义
        /// </summary>
        /// <param name="type">类型定义</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        public static PropertyDefinition GetProperty(this TypeDefinition type, string propertyName)
        {
            return type.Properties.FirstOrDefault(p => p.Name == propertyName);
        }

        /// <summary>
        /// 获取一个字段定义
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static FieldDefinition GetField(this TypeDefinition type, string fieldName)
        {
            return type.Fields.FirstOrDefault(p => p.Name == fieldName);
        }

        /// <summary>
        /// 从程序集定义中获取一个类型定义
        /// </summary>
        /// <param name="assembly">程序集定义</param>
        /// <param name="typeFullName">类型名</param>
        /// <returns></returns>
        public static TypeDefinition GetType(this AssemblyDefinition assembly, string typeFullName)
        {
            foreach (var module in assembly.Modules)
            {
                var type = module.GetType(typeFullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取一个方法的定义
        /// </summary>
        /// <param name="type">类型定义</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        public static MethodDefinition GetMethod(this TypeDefinition type, string methodName)
        {
            return type.Methods.FirstOrDefault(item => item.Name == methodName);
        }

        /// <summary>
        /// 是否拥有某个自定义特性
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="property">属性</param>
        /// <returns></returns>
        public static bool HasCustomAttribute<T>(this PropertyDefinition property) where T : System.Attribute
        {
            return property.CustomAttributes.FirstOrDefault(item => item.AttributeType.FullName == typeof(T).FullName) != null;
        }

        public static bool HasCustomAttribute(this PropertyDefinition property, string type)
        {
            return property.CustomAttributes.FirstOrDefault(item => item.AttributeType.FullName == type) != null;
        }
    }
}
