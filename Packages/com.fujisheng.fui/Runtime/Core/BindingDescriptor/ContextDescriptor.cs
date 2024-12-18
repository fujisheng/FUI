using FUI.Bindable;

using System;

namespace FUI.BindingDescriptor
{
    /// <summary>
    /// ������������ ����ֱ�Ӽ̳�
    /// </summary>
    public abstract class ContextDescriptor
    {
        /// <summary>
        /// ��������Ķ�Ӧ����ͼ����
        /// </summary>
        protected virtual string ViewName => string.Empty;

        /// <summary>
        /// ��������İ󶨵��������Ե�������
        /// </summary>
        protected virtual PropertyBindingDescriptor[] Properties => new PropertyBindingDescriptor[0];

        /// <summary>
        /// ��������İ󶨵����������������
        /// </summary>
        protected virtual CommandBindingDescriptor[] Commands => new CommandBindingDescriptor[0];


        protected PropertyBindingDescriptor BindingProperty(object property)
        {
            return new PropertyBindingDescriptor();
        }

        /// <summary>
        /// ���ڰ�ĳ��Event
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        protected CommandBindingDescriptor BindingCommand(string commandName)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand(Action command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs>(Action<TArgs> command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs1, TArgs2>(Action<TArgs1, TArgs2> command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs1, TArgs2, TArgs3>(Action<TArgs1, TArgs2, TArgs3> command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs1, TArgs2, TArgs3, TArgs4>(Action<TArgs1, TArgs2, TArgs3, TArgs4> command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5> command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6>(Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6> command)
        {
            return new CommandBindingDescriptor();
        }

        protected CommandBindingDescriptor BindingCommand<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6, TArgs7>(Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6, TArgs7> command)
        {
            return new CommandBindingDescriptor();
        }
    }

    /// <summary>
    /// ������������
    /// </summary>
    /// <typeparam name="TViewModel">��������Ķ�Ӧ��ViewModel����</typeparam>
    public abstract class ContextDescriptor<TViewModel> : ContextDescriptor where TViewModel : ObservableObject
    {
        /// <summary>
        /// ��������Ķ�Ӧ��ViewModel
        /// </summary>
        protected TViewModel VM { get; }
    }
}