using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    /// <summary>
    /// 可绑定属性
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBindableProperty<T>
    {
        T Value { get; set; }
        event ValueChangedHandler<T> OnValueChanged;
        void AddValueChanged(Delegate valueChanged);
        void RemoveValueChanged(Delegate valueChanged);
        void ClearValueChangedEvent();
        void MuteValueChangedEvent(bool mute);
        Delegate GetLastInvocation();
    }

    /// <summary>
    /// 只读的可绑定属性
    /// </summary>
    /// <typeparam name="T">属性值类型</typeparam>
    public interface IReadOnlyBindableProperty<out T>
    {
        T Value { get;}
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
    public class BindableProperty<T> : IBindableProperty<T>, IReadOnlyBindableProperty<T>, IWriteOnlyBindableProperty<T>
    {
        T value;

        bool eventMuted = false;

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
                if (this.eventMuted)
                {
                    return;
                }

                this.OnValueChanged?.Invoke(oldValue, value);
            }
        }

        public void AddValueChanged(Delegate valueChanged)
        {
            if(valueChanged == null)
            {
                return;
            }

            if (valueChanged is ValueChangedHandler<T> handler)
            {
                this.OnValueChanged += handler;
            }
        }

        public void RemoveValueChanged(Delegate valueChanged)
        {
            if(valueChanged == null)
            {
                return;
            }

            if(valueChanged is ValueChangedHandler<T> handler)
            {
                this.OnValueChanged -= handler;
            }
        }

        public Delegate GetLastInvocation()
        {
            if(this.OnValueChanged == null)
            {
                return null;
            }
            var invocationList = this.OnValueChanged.GetInvocationList();
            return invocationList.Length > 0 ? invocationList[invocationList.Length - 1] : null;
        }

        public void MuteValueChangedEvent(bool mute)
        {
            this.eventMuted = mute;
        }

        public void ClearValueChangedEvent()
        {
            this.OnValueChanged = null;
        }
    }
}