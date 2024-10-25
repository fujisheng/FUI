namespace FUI.Bindable
{
    public interface IReadonlyBindableProperty<out T>
    {
        T Value { get;}
    }

    /// <summary>
    /// 属性获取委托
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    /// <returns></returns>
    public delegate T PropertyGetHandler<out T>();

    /// <summary>
    /// 属性设置委托
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    /// <param name="value">属性值</param>
    public delegate void PropertySetHandler<in T>(T oldValue, T newValue);

    /// <summary>
    /// 可绑定的属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public class BindableProperty<T> : IReadonlyBindableProperty<T>
    {
        T value;

        /// <summary>
        /// 绑定类型
        /// </summary>
        public BindingType BindingType { get; }

        /// <summary>
        /// 这个属性的值
        /// </summary>
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// 属性值更改事件
        /// </summary>
        public event PropertySetHandler<T> PropertySet;

        /// <summary>
        /// 属性值获取委托
        /// </summary>
        public event PropertyGetHandler<T> PropertyGet;

        public BindableProperty()
        {
            this.BindingType = BindingType.OneWay;
        }

        public BindableProperty(T value)
        {
            this.Value = value;
            this.BindingType = BindingType.OneWay;
        }

        public BindableProperty(BindingType bindingType)
        {
            this.BindingType = bindingType;
        }

        public BindableProperty(T value, BindingType bindingType)
        {
            this.Value = value;
            this.BindingType = bindingType;
        }

        public BindableProperty(PropertySetHandler<T> setHandler, PropertyGetHandler<T> getHandler)
        {
            this.PropertySet = setHandler;
            this.PropertyGet = getHandler;
        }

        T GetValue()
        {
            return this.PropertyGet != null ? this.PropertyGet.Invoke() : this.value;
        }

        void SetValue(T value)
        {
            var oldValue = GetValue();
            if (oldValue == null || !value.Equals(oldValue))
            {
                this.PropertySet?.Invoke(oldValue, value);

                this.value = value;
            }
        }

        public void ClearEvent()
        {
            this.PropertySet = null;
            this.PropertyGet = null;
        }
    }
}