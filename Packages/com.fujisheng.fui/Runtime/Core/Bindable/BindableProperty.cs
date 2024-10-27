using System.Runtime.CompilerServices;

namespace FUI.Bindable
{
    /// <summary>
    /// ֻ���Ŀɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IReadonlyBindableProperty<out T>
    {
        T Value { get;}
        T GetValue();
    }

    /// <summary>
    /// ֻд�Ŀɰ�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public interface IWriteonlyBindableProperty<in T>
    {
        T Value { set; }
        void SetValue(T value);
        void SetValue(object value, string exception = null);
        void SetValue<TSet>(TSet value, string exception = null);
    }

    /// <summary>
    /// ���Ի�ȡί��
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    /// <returns></returns>
    public delegate T PropertyGetHandler<out T>();

    /// <summary>
    /// ��������ί��
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    /// <param name="value">����ֵ</param>
    public delegate void PropertySetHandler<in T>(T oldValue, T newValue);

    /// <summary>
    /// �ɰ󶨵�����
    /// </summary>
    /// <typeparam name="T">����ֵ����</typeparam>
    public class BindableProperty<T> : IReadonlyBindableProperty<T>, IWriteonlyBindableProperty<T>
    {
        T value;

        /// <summary>
        /// ������
        /// </summary>
        public BindingType BindingType { get; }

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
        public event PropertySetHandler<T> PropertySet;

        /// <summary>
        /// ����ֵ��ȡί��
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

        public T GetValue()
        {
            return this.PropertyGet != null ? this.PropertyGet.Invoke() : this.value;
        }

        public void SetValue(T value)
        {
            var oldValue = GetValue();

            if (oldValue == null || value == null || !value.Equals(oldValue))
            {
                this.PropertySet?.Invoke(oldValue, value);
                this.value = value;
            }
        }

        public void SetValue<TSet>(TSet value, string exception = null)
        {
            if(value == null)
            {
                SetValue(default);
                return;
            }

            if (!(value is T tValue))
            {
                if (string.IsNullOrEmpty(exception))
                {
                    throw new System.Exception($"can not convert {typeof(TSet)} to {typeof(T)}");
                }

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
            this.PropertySet = null;
            this.PropertyGet = null;
        }
    }
}