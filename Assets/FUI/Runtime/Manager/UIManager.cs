using FUI.Bindable;

using System.Threading;
using System.Threading.Tasks;

namespace FUI
{
    /// <summary>
    /// 层级
    /// </summary>
    public enum Layer
    {
        /// <summary>
        /// 背景层
        /// </summary>
        Background = 0,
        /// <summary>
        /// 普通层
        /// </summary>
        Common = 100,
        /// <summary>
        /// 前景层
        /// </summary>
        Foreground = 200,
        /// <summary>
        /// 顶层
        /// </summary>
        Top = 300,
    }

    /// <summary>
    /// 界面类型
    /// </summary>
    public enum ViewType
    {
        /// <summary>
        /// 普通界面
        /// </summary>
        Normal,
        /// <summary>
        /// 弹窗
        /// </summary>
        Popup,
        /// <summary>
        /// 固定界面
        /// </summary>
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
        /// <summary>
        /// 界面命令队列  保证界面打开关闭的顺序的同时可以同时加载多个界面
        /// </summary>
        ViewCommandQueue commandQueue;

        /// <summary>
        /// View构造器
        /// </summary>
        IViewBuilder builder;

        public UIManager(IViewBuilder builder)
        {
            this.builder = builder;
            this.viewStack = new ViewStack();
            this.commandQueue = new ViewCommandQueue();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize()
        {
            EnsureBuilder();
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
            OpenView(new OpenViewCommand(new ViewBuildParam(viewName), builder, false));
        }

        /// <summary>
        /// 通过默认的ViewModel异步打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        public async Task OpenAsync(string viewName, object param = null)
        {
            OpenView(new OpenViewCommand(new ViewBuildParam(viewName), builder, true));
            await WaitingForAllCommandExecuteComplete();
        }

        /// <summary>
        /// 通过指定的ViewModel打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            OpenView(new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel)), builder, false));
        }

        /// <summary>
        /// 通过指定的ViewModel异步打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async Task OpenAsync<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            OpenView(new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel)), builder, true));
            await WaitingForAllCommandExecuteComplete();
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            OpenView(new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel), typeof(TBehavior)), builder, false));
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来异步打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async Task OpenAsync<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            OpenView(new OpenViewCommand(new ViewBuildParam(viewName, typeof(TViewModel), typeof(TBehavior)), builder, true));
            await WaitingForAllCommandExecuteComplete();
        }

        void OpenView(OpenViewCommand command)
        {
            EnsureBuilder();
            foreach (var item in commandQueue.Commands)
            {
                //已经在打开中了 直接返回
                if((item is OpenViewCommand) && item.ViewName == command.ViewName)
                {
                    return;
                }

                //已经在关闭中了 取消关闭 并直接打开已有的
                if((item is CloseViewCommand) && item.ViewName == command.ViewName)
                {
                    Topping(command.ViewName);
                    item.Cancel();
                    commandQueue.Remove(item);
                    return;
                }
            }

            command.Execute(viewStack);
            commandQueue.Enqueue(command);
            OnUpdate();
        }

        /// <summary>
        /// 将某个View置顶
        /// </summary>
        /// <param name="viewName"></param>
        public void Topping(string viewName)
        {
            viewStack.Topping(viewName);
            //TODO 层级调整
        }

        public void OnUpdate()
        {
            if(commandQueue.Count == 0)
            {
                return;
            }

            var peek = commandQueue.Peek();
            if(!peek.IsCompleted)
            {
                return;
            }

            var command = commandQueue.Dequeue();
            command.Complete(viewStack);
        }

        /// <summary>
        /// 关闭一个界面
        /// </summary>
        public void Close(string viewName)
        {
            //如果要关闭的这个界面正在打开中 则直接取消要打开的那个界面
            bool isOpening = false;
            foreach (var command in commandQueue.Commands)
            {
                if ((command is OpenViewCommand) && command.ViewName == viewName)
                {
                    command.Cancel();
                    commandQueue.Remove(command);
                    isOpening = true;
                    break;
                }
            }
            if(isOpening)
            {
                return;
            }

            //要关闭的界面没有被打开
            var container = viewStack.GetContainer(viewName);
            if(container == null)
            {
                return;
            }

            commandQueue.Enqueue(new CloseViewCommand(viewName));
            OnUpdate();
        }

        void EnsureBuilder()
        {
            if(builder == null)
            {
                throw new System.Exception($"{nameof(builder)} is null, please call {nameof(SetViewBuilder)} first");
            }
        }

        /// <summary>
        /// 返回上一个界面
        /// </summary>
        public void Back()
        {
            var peek = viewStack.Peek();
            if(peek == null)
            {
                return;
            }

            Close(peek.Name);
        }

        /// <summary>
        /// 等待所有命令执行完毕
        /// </summary>
        /// <returns></returns>
        public async Task WaitingForAllCommandExecuteComplete()
        {
            if(commandQueue.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                while(commandQueue.Count > 0)
                {
                    Thread.Sleep(1);
                }
            });
        }
    }
}
