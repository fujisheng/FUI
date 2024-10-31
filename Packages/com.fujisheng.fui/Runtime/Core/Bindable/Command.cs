using System;

namespace FUI.Bindable
{
    public class Command<T>
    {
        Action<T> execute;

        public void AddListener(Action<T> action)
        {
            execute += action;
        }

        public void ClearListener()
        {
            execute = null;
        }

        public void Execute(T parameter)
        {
            execute?.Invoke(parameter);
        }
    }
}