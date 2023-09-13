using Newtonsoft.Json;

using System.Text;

namespace FUISourcesGenerator
{
    public partial class DataBindingContextGenerator : IBeforeCompilerSourcesGenerator
    {
        string configPath;
        string configExtension;

        private DataBindingContextGenerator() { }

        public DataBindingContextGenerator(string configPath, string configExtension)
        {
            this.configPath = configPath;
            this.configExtension = configExtension;
        }

        Source?[] IBeforeCompilerSourcesGenerator.Generate()
        {
            var result = new List<Source?>();
            foreach (var file in Directory.GetFiles(configPath, $"*{configExtension}"))
            {
                var config = JsonConvert.DeserializeObject<BindingConfig>(File.ReadAllText(file));
                var code = Template.Replace(ViewNameMark, config.viewName);

                var bindingBuilder = new StringBuilder();
                HashSet<string> converterTypes = new HashSet<string>();
                HashSet<string> propertyDelegates = new HashSet<string>();
                foreach (var bindingContext in config.contexts)
                {
                    var bindingItemsBuilder = new StringBuilder();
                    var vmName = GetFormattedType(bindingContext.type);
                    
                    foreach(var property in bindingContext.properties)
                    {
                        var delegateName = Utility.GetPropertyChangedDelegateName(property.name);
                        propertyDelegates.Add(delegateName);

                        var propertyItem = BindingItemTemplate.Replace(PropertyChangedDelegateMark, delegateName);
                        propertyItem = propertyItem.Replace(ViewModelNameMark, vmName);
                        var convert = BuildConvert(bindingContext.type, property);
                        propertyItem = propertyItem.Replace(ConvertMark, convert);
                        propertyItem = propertyItem.Replace(ElementTypeMark, property.elementType);
                        propertyItem = propertyItem.Replace(ElementPathMark, property.elementPath);
                        bindingItemsBuilder.AppendLine(propertyItem);

                        converterTypes.Add(property.converterType);
                    }
                    var bindingItem = BindingTemplate.Replace(ViewModelTypeMark, bindingContext.type);
                    bindingItem = bindingItem.Replace(ViewModelNameMark, vmName);
                    bindingItem = bindingItem.Replace(BindingItemsMark, bindingItemsBuilder.ToString());
                    bindingBuilder.AppendLine(bindingItem);
                }
                code = code.Replace(BindingMark, bindingBuilder.ToString());

                var convertersBuilder = new StringBuilder();
                foreach (var converterType in converterTypes)
                {
                    if (string.IsNullOrEmpty(converterType))
                    {
                        continue;
                    }
                    convertersBuilder.AppendLine($"{converterType} {GetFormattedType(converterType)} = new {converterType}();");
                }
                code = code.Replace(ConvertersMark, convertersBuilder.ToString());

                var unbindingBuilder = new StringBuilder();
                foreach (var propertyDelegate in propertyDelegates)
                {
                    unbindingBuilder.AppendLine($"{propertyDelegate} = null;");
                }
                code = code.Replace(UnbindingMark, string.Empty); // unbindingBuilder.ToString());

                code = Utility.NormalizeCode(code);
                Console.WriteLine($"generate data binding for {config.viewName}");
                result.Add(new Source($"{config.viewName}.DataBinding", code));
            }

            return result.ToArray();
        }

        /// <summary>
        /// 构建值转换器
        /// </summary>
        string BuildConvert(string viewModelType, BindingProperty property)
        {
            var convertBuilder = new StringBuilder();
            var elementValueType = Utility.GetGenericTypeName(property.elementValueType);
            if (string.IsNullOrEmpty(property.converterType))
            {
                convertBuilder.AppendLine($"if(!(@value is {elementValueType} convertedValue))");
                convertBuilder.AppendLine("{");
                convertBuilder.AppendLine($"throw new System.Exception(\"{viewModelType}.{property.name} can not convert to {property.elementValueType} property:{property.type} element:{property.elementType} elementValueType:{property.elementValueType}\");");
                convertBuilder.AppendLine("}");
            }
            else
            {
                convertBuilder.AppendLine($"var convertedTempValue = {GetFormattedType(property.converterType)}.Convert(@value);");
                convertBuilder.AppendLine($"if(!(convertedTempValue is {elementValueType} convertedValue))");
                convertBuilder.AppendLine("{");
                convertBuilder.AppendLine($"throw new System.Exception(\"{viewModelType}.{property.name} can not convert to {property.elementValueType} property:{property.type} converter:{property.converterType}({property.type},{property.converterTargetType}) element:{property.elementType}({property.elementValueType})\");");
                convertBuilder.AppendLine("}");
            }
            return convertBuilder.ToString();
        }

        static string GetFormattedType(string type)
        {
            return type.Replace(".", "_");
        }
    }
}
