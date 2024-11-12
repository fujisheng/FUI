using System;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace FUI.Bindable
{
    /// <summary>
    /// �ɰ�����
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBindableProperty<T> : IDisposable
    {
        T Value { get; set; }
        event ValueChangedHandler<T> OnValueChanged;
        void AddValueChanged(Delegate valueChanged);
        void RemoveValueChanged(Delegate valueChanged);
        void MuteValueChangedEvent(bool mute);
        Delegate GetLastInvocation();
    }

    /// <summary>
    /// ֻ���Ŀɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IReadOnlyBindableProperty<out T>
    {
        T Value { get;}
        T GetValue();
    }

    /// <summary>
    /// ֻд�Ŀɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IWriteOnlyBindableProperty<in T>
    {
        T Value { set; }
        void SetValue(T value, string exception);
        void SetValue(object value, string exception);
    }

    /// <summary>
    /// ֵ�ı�ί��
    /// </summary>
    /// <typeparam name="T">ֵ����</typeparam>
    /// <param name="newValue">ֵ</param>
    /// <param name="oldValue">�ı�֮ǰ��ֵ</param>
    public delegate void ValueChangedHandler<in T>(T oldValue, T newValue);

    /// <summary>
    /// �ɰ󶨵�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public class BindableProperty<T> : IBindableProperty<T>, IReadOnlyBindableProperty<T>, IWriteOnlyBindableProperty<T>
    {
        T value;

        bool eventMuted = false;

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

        public void SetValue(T value, string exception = null)
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

        public void SetValue(object value, string exception = null)
        {
            if (EqualityComparer<object>.Default.Equals(value, default))
            {
                SetValue(default);
                return;
            }

            if (!(value is T tValue))
            {
                exception = exception ?? $"can not convert {value.GetType()} to {typeof(T)}";
                throw new System.Exception(exception);
            }

            SetValue(tValue);
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

        public void Dispose()
        {
            this.OnValueChanged = null;
            this.value = default;
        }
    }
}