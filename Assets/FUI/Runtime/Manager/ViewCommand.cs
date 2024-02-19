using FUI.Bindable;

using System.Collections.Generic;
using System.Threading;

namespace FUI
{
    /// <summary>
    /// 界面命令队列
    /// </summary>
    class ViewCommandQueue
    {
        List<ViewCommand> commands;

        /// <summary>
        /// 所有的命令
        /// </summary>
        internal IEnumerable<ViewCommand> Commands => commands;

        /// <summary>
        /// 初始化一个队列
        /// </summary>
        internal ViewCommandQueue()
        {
            commands = new List<ViewCommand>();
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="command">入队的命令</param>
        internal void Enqueue(ViewCommand command)
        {
            commands.Add(command);
        }

        /// <summary>
        /// 出队
        /// </summary>
        /// <returns>出队的命令</returns>
        internal ViewCommand Dequeue()
        {
            if (commands.Count == 0)
            {
                return null;
            }
            var command = commands[0];
            commands.RemoveAt(0);
            return command;
        }

        /// <summary>
        /// 获取最前面的命令
        /// </summary>
        /// <returns></returns>
        internal ViewCommand Peek()
        {
            if (commands.Count == 0)
            {
                return null;
            }
            return commands[0];
        }

        /// <summary>
        /// 移除一个命令
        /// </summary>
        /// <param name="command"></param>
        internal void Remove(ViewCommand command)
        {
            commands.Remove(command);
        }

        /// <summary>
        /// 清空所有命令
        /// </summary>
        internal void Clear()
        {
            commands.Clear();
        }

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => commands.Count;

        /// <summary>
        /// 是否存在某个命令
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal bool Exist(string viewName)
        {
            return commands.Exists(c => c.ViewName == viewName);
        }
    }

    /// <summary>
    /// 界面命令
    /// </summary>
    abstract class ViewCommand
    {
        internal abstract string ViewName { get; set; }
        internal abstract bool IsCompleted { get; set; }
        internal abstract UIContext Result { get; set; }
        internal abstract void Execute(UIStack viewStack);
        internal abstract void Complete(UIStack viewStack);
        internal abstract void Cancel();
    }

    class OpenViewCommand : ViewCommand
    {
        UIBuildParam param;
        IUIBuilder builder;
        CancellationTokenSource cancellationTokenSource;

        internal override string ViewName { get; set; }
        internal override UIContext Result { get; set; }
        internal override bool IsCompleted { get; set; }
        bool isAsync;

        internal OpenViewCommand(UIBuildParam param, IUIBuilder builder, bool isAsync)
        {
            this.ViewName = param.viewName;
            this.param = param;
            this.builder = builder;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.IsCompleted = false;
            this.isAsync = isAsync;
        }

        internal async override void Execute(UIStack viewStack)
        {
            IView view;
            ObservableObject viewModel;
            ViewBehavior viewBehavior;

            if (isAsync)
            {
                (view, viewModel, viewBehavior) = await builder.BuildViewAsync(param, cancellationTokenSource.Token);
            }
            else
            {
                view = builder.BuildView(param, out viewModel, out viewBehavior);
            }

            if (view == null)
            {
                IsCompleted = true;
                UnityEngine.Debug.LogWarning($"open view:{param.viewName} failed");
                return;
            }
            Result = UIContext.Create(view, viewModel, viewBehavior);
            IsCompleted = true;
        }

        internal override void Complete(UIStack viewStack)
        {
            if (!IsCompleted)
            {
                return;
            }

            if (Result == null)
            {
                return;
            }

            Result.Open(null);
            viewStack.Push(Result);
            UnityEngine.Debug.Log($"打开界面完成：{ViewName}");
        }

        internal override void Cancel()
        {
            UnityEngine.Debug.Log($"取消打开界面：{ViewName}");
            if (!IsCompleted)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            Result?.Destroy();
        }
    }

    class CloseViewCommand : ViewCommand
    {
        internal override string ViewName { get; set; }
        internal override bool IsCompleted { get; set; }
        internal override UIContext Result { get; set; }
        internal CloseViewCommand(string viewName)
        {
            this.ViewName = viewName;
        }

        internal override void Execute(UIStack viewStack)
        {
            Result = viewStack.GetContext(ViewName);
            IsCompleted = true;
        }

        internal override void Cancel()
        {
            Result = null;
            UnityEngine.Debug.Log($"取消关闭界面：{ViewName}");
        }

        internal override void Complete(UIStack viewStack)
        {
            if (Result == null)
            {
                return;
            }

            Result.Close();
            viewStack.Remove(Result);
            UnityEngine.Debug.Log($"关闭界面完成：{ViewName}");
            //TODO是否缓存被关闭的界面
        }
    }
}
