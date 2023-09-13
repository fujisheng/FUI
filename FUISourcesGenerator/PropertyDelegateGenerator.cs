using Mono.Cecil;

using System.Text;

namespace FUISourcesGenerator
{
    /// <summary>
    /// 为可监听的属性生成对应的委托
    /// </summary>
    internal class PropertyDelegateGenerator : ITypeDefinationSourcesGenerator
    {
        public Source?[] GetSource(ModuleDefinition moduleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.HasCustomAttribute(Utility.ObservableObjectFullName))
            {
                return null;
            }
            var properties = typeDefinition.Properties;
            if(properties.Count == 0)
            {
                return null;
            }

            var propertyDelegateBuilder = new StringBuilder();
            var hasNamespace = !string.IsNullOrEmpty(typeDefinition.Namespace);
            if (hasNamespace)
            {
                propertyDelegateBuilder.AppendLine($"namespace {typeDefinition.Namespace}");
                propertyDelegateBuilder.AppendLine("{");
            }
            
            propertyDelegateBuilder.AppendLine($"public partial class {typeDefinition.Name}");
            propertyDelegateBuilder.AppendLine("{");

            var observableProperties = new List<PropertyDefinition>();
            foreach(var property in properties)
            {
                var isObservableProperty = property.HasCustomAttribute(Utility.ObservablePropertyFullName);
                if (!isObservableProperty)
                {
                    continue;
                }
                observableProperties.Add(property);
            }

            if(observableProperties.Count == 0)
            {
                return null;
            }

            var generatedTypes = new HashSet<string>();
            foreach(var property in observableProperties)
            {
                var propertyType = Utility.GetGenericTypeName(property.PropertyType.FullName);
                var formattedTypeName = Utility.TypeToCSharpName(propertyType);
                string delegateType = $"PropertyChangedHandler_{formattedTypeName}";
                //生成对应的委托声明  这儿放在里面并且不是泛型是为了后面注入il代码的时候不用那么麻烦
                if (!generatedTypes.Contains(delegateType))
                {
                    propertyDelegateBuilder.AppendLine($"public delegate void {delegateType}(object sender, {propertyType} preValue, {propertyType} newValue);");
                    generatedTypes.Add(delegateType);
                }
                
                //生成对应的委托
                var delegateName = Utility.GetPropertyChangedDelegateName(property.Name);
                propertyDelegateBuilder.AppendLine($"public {delegateType} {delegateName};");
            }

            propertyDelegateBuilder.AppendLine("}");
            if (hasNamespace)
            {
                propertyDelegateBuilder.AppendLine("}");
            }
            var code = Utility.NormalizeCode(propertyDelegateBuilder.ToString());
            Console.WriteLine($"generate property changed delegate for {typeDefinition}");
            return new Source?[] {new Source($"{typeDefinition.FullName}.PropertyChanged", code)};
        }
    }
}
