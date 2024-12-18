using FUI.Bindable;

using System;

namespace FUI.BindingDescriptor
{
    /// <summary>
    /// 上下文描述器 不可直接继承
    /// </summary>
    public abstract class ContextDescriptor
    {
        /// <summary>
        /// 这个上下文对应的视图名字
        /// </summary>
        protected virtual string ViewName => string.Empty;

        /// <summary>
        /// 这个上下文绑定的所有属性的描述器
        /// </summary>
        protected virtual PropertyBindingDescriptor[] Properties => new PropertyBindingDescriptor[0];

        /// <summary>
        /// 这个上下文绑定的所有命令的描述器
        /// </summary>
        protected virtual CommandBindingDescriptor[] Commands => new CommandBindingDescriptor[0];


        protected PropertyBindingDescriptor BindingProperty(object property)
        {
            return new PropertyBindingDescriptor();
        }

        /// <summary>
        /// 用于绑定某个Event
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
    /// 上下文描述器
    /// </summary>
    /// <typeparam name="TViewModel">这个上下文对应的ViewModel类型</typeparam>
    public abstract class ContextDescriptor<TViewModel> : ContextDescriptor where TViewModel : ObservableObject
    {
        /// <summary>
        /// 这个上下文对应的ViewModel
        /// </summary>
        protected TViewModel VM { get; }
    }
}