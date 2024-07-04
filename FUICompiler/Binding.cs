namespace FUICompiler
{
    public class TypeInfo
    {
        public string name;
        public string fullName;
        public bool isGenericType;
        public bool isValueType;
        public List<TypeInfo> genericArguments = new List<TypeInfo>();

        public static TypeInfo Create(Type type)
        {
            if (type == null)
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

    public class BindingProperty
    {
        public string name;
        public TypeInfo type;
        public TypeInfo converterType;
        public string elementPath;
        public TypeInfo elementType;

        public TypeInfo converterValueType;
        public TypeInfo converterTargetType;
        public TypeInfo elementValueType;
    }

    public class BindingContext
    {
        public string type;
        public List<BindingProperty> properties = new List<BindingProperty>();
    }

    public class BindingConfig
    {
        public string viewName = "TestView";
        public List<BindingContext> contexts = new List<BindingContext>();
    }
}
