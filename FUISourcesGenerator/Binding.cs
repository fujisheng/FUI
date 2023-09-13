namespace FUISourcesGenerator
{
    public class BindingProperty
    {
        public string name;
        public string type;
        public string converterType;
        public string elementPath;
        public string elementType;

        public string converterTargetType;
        public string elementValueType;
    }

    public class BindingContext
    {
        public string type;
        public List<BindingProperty> properties;
    }

    public class BindingConfig
    {
        public string viewName;
        public List<BindingContext> contexts;
        //public BindingContext defaultContext;
    }
}
