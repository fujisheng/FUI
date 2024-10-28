using System.Collections.Generic;

namespace FUI.Bindable
{
    /// <summary>
    /// 只读的可绑定属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public interface IReadonlyBindableProperty<out T>
    {
        T Value { get;}
        T GetValue();
    }

    /// <summary>
    /// 只写的可绑定属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public interface IWriteonlyBindableProperty<in T>
    {
        T Value { set; }
        void SetValue(T value);
        void SetValue(object value, string exception = null);
        void SetValue<TSet>(TSet value, string exception = null);
    }

    /// <summary>
    /// 值改变委托
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="newValue">值</param>
    /// <param name="oldValue">改变之前的值</param>
    public delegate void ValueChangedHandler<in T>(T oldValue, T newValue);

    /// <summary>
    /// 可绑定的属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public class BindableProperty<T> : IReadonlyBindableProperty<T>, IWriteonlyBindableProperty<T>
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
        public event ValueChangedHandler<T> OnValueChanged;

        public BindableProperty()
        {
            this.BindingType = BindingType.OneWay;
        }

        public BindableProperty(T value)
        {
            this.value = value;
            this.BindingType = BindingType.OneWay;
        }

        public BindableProperty(BindingType bindingType)
        {
            this.BindingType = bindingType;
        }

        public BindableProperty(T value, BindingType bindingType)
        {
            this.value = value;
            this.BindingType = bindingType;
        }

        public BindableProperty(ValueChangedHandler<T> onValueChanged)
        {
            this.OnValueChanged = onValueChanged;
            this.BindingType = BindingType.OneWay;
        }

        public T GetValue()
        {
            return this.value;
        }

        public void SetValue(T value)
        {
            var oldValue = GetValue();

            if (!EqualityComparer<T>.Default.Equals(oldValue, value))
            {
                this.value = value;
                this.OnValueChanged?.Invoke(oldValue, value);
            }
        }

        public void SetValue<TSet>(TSet value, string exception = null)
        {
            if(EqualityComparer<TSet>.Default.Equals(value, default))
            {
                SetValue(default);
                return;
            }

            if (!(value is T tValue))
            {
                exception = exception ?? $"can not convert {typeof(TSet)} to {typeof(T)}";
                throw new System.Exception(exception);
            }

            SetValue(tValue);
        }

        public void SetValue(object value, string exception = null)
        {
            SetValue<object>(value, exception);
        }

        public void ClearEvent()
        {
            this.OnValueChanged = null;
        }
    }
}