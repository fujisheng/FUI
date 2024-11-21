using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    /// <summary>
    /// ֵ�ı�ί��
    /// </summary>
    /// <typeparam name="T">ֵ����</typeparam>
    /// <param name="newValue">ֵ</param>
    /// <param name="oldValue">�ı�֮ǰ��ֵ</param>
    public delegate void ValueChangedHandler<in T>(T oldValue, T newValue);

    /// <summary>
    /// ֻ���Ŀɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IReadOnlyBindableProperty<out T>
    {
        T Value { get; }
        T GetValue();
    }

    /// <summary>
    /// ֻд�Ŀɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IWriteOnlyBindableProperty<in T>
    {
        T Value { set; }
        void SetValue(T value);
        void SetValue(object value);
    }

    /// <summary>
    /// �ɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IBindableProperty<T> : IReadOnlyBindableProperty<T>, IWriteOnlyBindableProperty<T>
    {
        event ValueChangedHandler<T> OnValueChanged;
    }

    /// <summary>
    /// �ɰ󶨵�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public class BindableProperty<T> : IBindableProperty<T>, IDisposable
    {
        T value;

        /// <summary>
        /// ������Ե�ֵ
        /// </summary>
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// ����ֵ�����¼�
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