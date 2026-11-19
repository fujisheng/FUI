using System;
using FUI.Binding;
using URUI.Events;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 统一原生值控件的静默写入、规范化和用户事件顺序。
    /// </summary>
    public abstract class ValueElement<T> : ElementAdapter
    {
        readonly global::URUI.Controls.INotifyValueChanged<T> field;
        readonly IDisposable subscription;
        int writeDepth;
        bool initialized;
        bool disposed;

        protected ValueElement(global::URUI.Elements.Element element, string name)
            : this(element, name, element as global::URUI.Controls.INotifyValueChanged<T>) { }

        protected ValueElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.INotifyValueChanged<T> source) : base(RequireSource(element, source), name)
        {
            field = source;
            Value = new BindableProperty<T>(field.Value, null, Normalize);
            initialized = true;
            ValueChanged = new Command<T>();
            subscription = element.RegisterCallback<ChangeEvent<T>>(evt =>
            {
                if (writeDepth != 0 || evt.Target != element)
                {
                    return;
                }
                var value = evt.NewValue;
                element.Document.RunAfterEvent(() =>
                {
                    if (disposed || Element == null || !Element.IsValid)
                    {
                        return;
                    }
                    Value.Value = value;
                    if (!disposed && Element != null && Element.IsValid)
                    {
                        ValueChanged.Invoke(Value.Value);
                    }
                });
            });
        }
        static global::URUI.Elements.Element RequireSource(global::URUI.Elements.Element element, global::URUI.Controls.INotifyValueChanged<T> source)
        {
            if (source == null)
            {
                throw new ArgumentException("值元素需要对应的原生值控件。", nameof(source));
            }
            return element;
        }

        public BindableProperty<T> Value
        {
            get;
        }
        public Command<T> ValueChanged
        {
            get;
        }

        T Normalize(T value)
        {
            // 构造初值已由原生控件规范化，不写回以保留声明属性。
            if (!initialized)
            {
                return value;
            }
            writeDepth++;
            try
            {
                field.SetValueWithoutNotify(value);
                return field.Value;
            }
            finally
            {
                writeDepth--;
            }
        }

        protected void Configure(Action action)
        {
            writeDepth++;
            try
            {
                action();
            }
            finally
            {
                writeDepth--;
            }
            Value.Value = field.Value;
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            subscription.Dispose();
            Value.Dispose();
            ValueChanged.Dispose();
            base.Dispose();
        }
    }
}
