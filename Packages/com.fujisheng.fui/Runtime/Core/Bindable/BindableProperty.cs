namespace FUI.Bindable
{
    public interface IReadonlyBindableProperty<out T>
    {
        T Value { get;}
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
    public class BindableProperty<T> : IReadonlyBindableProperty<T>
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