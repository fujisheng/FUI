using FUI.Bindable;

using System.Collections.Generic;
using System.Threading;

namespace FUI
{
    internal abstract class ViewStack
    {
        internal int Count { get; }
        internal abstract Container Peek();
        internal abstract Container Pop();
        internal abstract void Push(Container view);

        internal abstract Container GetView(string viewName);
        internal abstract void Remove(Container view);
        internal abstract void Clear();
    }

    internal class OpeningQueue
    {
        List<OpenViewCommand> commands;

        public IEnumerable<OpenViewCommand> Commands => commands;

        internal void Enqueue(OpenViewCommand command)
        {
            commands.Add(command);
        }

        internal OpenViewCommand Dequeue()
        {
            if (commands.Count == 0)
            {
                return null;
            }
            var command = commands[0];
            commands.RemoveAt(0);
            return command;
        }

        internal OpenViewCommand Peek()
        {
            if (commands.Count == 0)
            {
                return null;
            }
            return commands[0];
        }

        internal void Remove(OpenViewCommand command)
        {
            commands.Remove(command);
        }

        internal void Clear()
        {
            commands.Clear();
        }

        internal int Count => commands.Count; 

        internal bool Exist(string viewName)
        {
            return commands.Exists(c => c.ViewName == viewName);
        }
    }

    internal class OpenViewCommand
    {
        ViewBuildParam param;
        IViewBuilder builder;
        CancellationTokenSource cancellationTokenSource;
        bool isAsync;

        internal string ViewName { get; private set; }
        internal Container Result { get; private set; }
        internal bool IsCompleted { get; private set; }

        internal OpenViewCommand(ViewBuildParam param, IViewBuilder builder, bool isAsync)
        {
            this.ViewName = param.viewName;
            this.param = param;
            this.builder = builder;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.isAsync = isAsync;
            this.IsCompleted = false;
        }

        internal async void Execute(ViewStack viewStack)
        {
            View view;
            ObservableObject viewModel;
            ViewBehavior viewBehavior;

            if (isAsync)
            {
                view = await builder.BuildViewAsync(param, cancellationTokenSource.Token, out viewModel, out viewBehavior);
            }
            else
            {
                view = builder.BuildView(param, out viewModel, out viewBehavior);
            }
            
            if (view == null)
            {
                UnityEngine.Debug.LogWarning($"open view:{param.viewName} failed");
                return;
            }
            Result = Container.Create(view, viewModel, viewBehavior);
            IsCompleted = true;
        }

        internal void Cancel()
        {
            if (!IsCompleted)
            {
                cancellationTokenSource.Cancel();
                return;
            }
            
            Result.Destroy();
        }
    }

    public enum Layer
    {
        Background,
        Common,
        Foreground,
        Top,
    }

    public enum ViewType
    {
        Normal,
        Popup,
        Fixed,
    }

    /// <summary>
    /// UI管理器
    /// </summary>
    public class UIManager
    {
        /// <summary>
        /// 所有打开的界面
        /// </summary>
        ViewStack viewStack;
        OpeningQueue openingQueue;

        /// <summary>
        /// View构造器
        /// </summary>
        IViewBuilder builder;

        public UIManager(IViewBuilder builder)
        {
            this.builder = builder;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize()
        {
            EnsureCreator();
        }

        /// <summary>
        /// 设置View构造器
        /// </summary>
        /// <param name="builder"></param>
        public void SetViewBuilder(IViewBuilder builder)
        {
            this.builder = builder;
        }

        /// <summary>
        /// 通过默认的ViewModel打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <param name="param">打开时的参数</param>
        public void Open(string viewName, object param = null)
        {
            EnsureCreator();
            var command = new OpenViewCommand(new ViewBuildParam(viewName), builder, false);
        }

        /// <summary>
        /// 通过默认的ViewModel异步打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        public void OpenAsync(string viewName, object param = null)
        {
            EnsureCreator();
            var command = new OpenViewCommand(new ViewBuildParam(viewName), builder, true);
        }

        /// <summary>
        /// 通过指定的ViewModel打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            EnsureCreator();
            var command = new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel)), builder, false);
        }

        /// <summary>
        /// 通过指定的ViewModel异步打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void OpenAsync<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            EnsureCreator();
            var commmand = new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel)), builder, true);
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            EnsureCreator();
            var command = new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel), typeof(TBehavior)), builder, false);
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来异步打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void OpenAsync<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            EnsureCreator();
            var command = new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel), typeof(TBehavior)), builder, true);
        }

        void OpenView(OpenViewCommand command)
        {
            if (openingQueue.Exist(command.ViewName))
            {
                return;
            }
            command.Execute(viewStack);
            openingQueue.Enqueue(command);
        }

        internal void OnUpdate()
        {
            if(openingQueue.Count == 0)
            {
                return;
            }

            var peek = openingQueue.Peek();
            if(!peek.IsCompleted)
            {
                return;
            }

            var container = openingQueue.Dequeue().Result;
            container.Open(null);
            viewStack.Push(container);
            //TODO 界面打开完成 此时应该判断它入栈后是否应该隐藏掉后面的所有界面
        }

        public void Close(string viewName)
        {
            //如果要关闭的这个界面正在打开中 则直接取消要打开的那个界面
            bool isOpening = false;
            foreach (var command in openingQueue.Commands)
            {
                if (command.ViewName == viewName)
                {
                    command.Cancel();
                    openingQueue.Remove(command);
                    isOpening = true;
                    break;
                }
            }
            if(isOpening)
            {
                return;
            }

            //要关闭的界面没有被打开
            var container = viewStack.GetView(viewName);
            if(container == null)
            {
                return;
            }

            
        }

        void EnsureCreator()
        {
            if(builder == null)
            {
                throw new System.Exception("viewCreator is null, please call SetCreator first");
            }
        }

        /// <summary>
        /// 返回上一个界面
        /// </summary>
        public void Back()
        {

        }
    }
}
