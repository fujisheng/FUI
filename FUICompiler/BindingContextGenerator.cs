using Newtonsoft.Json;

using System.Text;

namespace FUICompiler
{
    public partial class BindingContextGenerator : IBeforeCompilerSourcesGenerator
    {
        string configPath;
        string configExtension;

        public BindingContextGenerator(string configPath, string configExtension)
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

                Generate(config, ref result);
            }

            return result.ToArray();
        }

        internal void Generate(BindingConfig config, ref List<Source?> result, IEnumerable<string> usings = null)
        {
            foreach (var bindingContext in config.contexts)
            {
                var code = Template.Replace(ViewNameMark, config.viewName);

                var bindingBuilder = new StringBuilder();
                var unbindingBuilder = new StringBuilder();
                HashSet<string> converterTypes = new HashSet<string>();
                HashSet<string> propertyDelegates = new HashSet<string>();
                var bingingFunctionsBuilder = new StringBuilder();

                //为每个上下文生成对应的绑定代码
                var bindingItemsBuilder = new StringBuilder();
                var unbindingItemsBuilder = new StringBuilder();
                var defaultViewModelType = bindingContext.type;
                var vmName = GetFormattedType(bindingContext.type);

                //为每个属性生成对应的委托 并添加绑定代码和解绑代码
                foreach (var property in bindingContext.properties)
                {
                    var delegateName = Utility.GetPropertyChangedDelegateName(property.name);
                    propertyDelegates.Add(delegateName);

                    //构建值转换
                    var convert = BuildConvert(bindingContext.type, property);

                    //为属性生成对应的绑定方法
                    var propertyChangedFunctionName = $"{vmName}_{property.name}_PropertyChanged";
                    var elementType = property.elementType.IsNull() ? string.Empty : $"<{property.elementType.ToTypeString()}>";
                    bingingFunctionsBuilder.AppendLine(BindingItemFunctionTemplate.Replace(PropertyChangedFunctionNameMark, propertyChangedFunctionName)
                        .Replace(PropertyNameMark, property.name))
                        .Replace(PropertyTypeMark, property.type.ToTypeString())
                        .Replace(ConvertMark, convert)
                        .Replace(ElementTypeMark, elementType)
                        .Replace(ElementPathMark, property.elementPath);

                    //生成属性绑定代码
                    bindingItemsBuilder.AppendLine($"{vmName}.{delegateName} += {propertyChangedFunctionName};");

                    //生成属性解绑代码
                    unbindingItemsBuilder.AppendLine($"{vmName}.{delegateName} -= {propertyChangedFunctionName};");

                    converterTypes.Add(property.converterType.ToTypeString());
                }

                //组装绑定代码
                bindingBuilder.AppendLine(BindingTemplate.Replace(ViewModelTypeMark, bindingContext.type)
                    .Replace(ViewModelNameMark, vmName)
                    .Replace(BindingItemsMark, bindingItemsBuilder.ToString()));

                //组装解绑代码
                unbindingBuilder.AppendLine(BindingTemplate.Replace(ViewModelTypeMark, bindingContext.type)
                    .Replace(ViewModelNameMark, vmName)
                    .Replace(BindingItemsMark, unbindingItemsBuilder.ToString()));

                //组装所有的绑定代码
                code = code.Replace(BindingMark, bindingBuilder.ToString())
                    .Replace(UnbindingMark, unbindingBuilder.ToString())
                    .Replace(PropertyChangedFunctionsMark, bingingFunctionsBuilder.ToString())
                    .Replace(DefaultViewModelTypeMark, defaultViewModelType)
                    .Replace(ViewModelNameMark, vmName);

                //生成所有的转换器构造代码
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

                //添加using
                var usingBuilder = new StringBuilder();
                if(usings == null)
                {
                    usingBuilder.AppendLine("");
                }
                else
                {
                    foreach(var @using in usings)
                    {
                        if (string.IsNullOrEmpty(@using))
                        {
                            continue;
                        }

                        usingBuilder.AppendLine($"using {@using};");
                    }
                }
                code = code.Replace(UsingMark, usingBuilder.ToString());

                //格式化代码
                code = Utility.NormalizeCode(code);
                //Console.WriteLine(code);
                Console.WriteLine($"generate data binding for {config.viewName}");
                result.Add(new Source($"{config.viewName}.DataBinding", code));
            }
        }

        /// <summary>
        /// 构建值转换器
        /// </summary>
        string BuildConvert(string viewModelType, BindingProperty property)
        {
            var convertBuilder = new StringBuilder();
            //convertBuilder.AppendLine($"{property.elementValueType.ToTypeString()} convertedValue;");
            if (!property.converterType.IsNull())
            {
                //convertBuilder.AppendLine($"var input = ({property.converterValueType.ToTypeString()})@value;");
                //convertBuilder.AppendLine($"var convertedTempValue = {GetFormattedType(property.converterType.ToTypeString())}.Convert(input);");
                //convertBuilder.AppendLine($"convertedValue = ({property.elementValueType.ToTypeString()})convertedTempValue;");
                convertBuilder.AppendLine($"var convertedValue = {GetFormattedType(property.converterType.ToTypeString())}.Convert(@value);");
            }
            else
            {
                convertBuilder.AppendLine($"var convertedValue = @value;");
                //convertBuilder.AppendLine($"convertedValue = ({property.elementValueType.ToTypeString()})@value;");
            }
            return convertBuilder.ToString();
        }

        static string GetFormattedType(string type)
        {
            return type.Replace(".", "_");
        }
    }
}
