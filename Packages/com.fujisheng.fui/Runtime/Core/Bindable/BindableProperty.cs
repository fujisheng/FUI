using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    /// <summary>
    /// 值改变委托
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="newValue">值</param>
    /// <param name="oldValue">改变之前的值</param>
    public delegate void ValueChangedHandler<in T>(T oldValue, T newValue);

    /// <summary>
    /// 只读的可绑定属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public interface IReadOnlyBindableProperty<out T>
    {
        T Value { get; }
        T GetValue();
    }

    /// <summary>
    /// 只写的可绑定属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public interface IWriteOnlyBindableProperty<in T>
    {
        T Value { set; }
        void SetValue(T value);
        void SetValue(object value);
    }

    /// <summary>
    /// 可绑定属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public interface IBindableProperty<T> : IReadOnlyBindableProperty<T>, IWriteOnlyBindableProperty<T>
    {
        event ValueChangedHandler<T> OnValueChanged;
    }

    /// <summary>
    /// 可绑定的属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public class BindableProperty<T> : IBindableProperty<T>, IDisposable
    {
        T value;

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

        public BindableProperty() { }

        public BindableProperty(T value, ValueChangedHandler<T> onValueChanged = null)
        {
            this.value = value;

            if(onValueChanged != null)
            {
                this.OnValueChanged += onValueChanged;
            }
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

        public void SetValue(object value)
        {
            if (EqualityComparer<object>.Default.Equals(value, default))
            {
                SetValue(default);
                return;
            }

            if (!(value is T tValue))
            {
                throw new System.Exception($"can not convert {value.GetType()} to {typeof(T)}");
            }

            SetValue(tValue);
        }

        public void Dispose()
        {
            this.OnValueChanged = null;
            this.value = default;
        }
    }
}