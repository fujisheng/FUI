using FUI.Bindable;

using System.Threading;
using System.Threading.Tasks;

namespace FUI.Manager
{
    /// <summary>
    /// UI管理器
    /// </summary>
    public class UIManager
    {
        /// <summary>
        /// 所有打开的界面
        /// </summary>
        readonly UIStack uiStack;
        /// <summary>
        /// 界面命令队列  保证界面打开关闭的顺序的同时可以同时加载多个界面
        /// </summary>
        readonly ViewCommandQueue commandQueue;

        /// <summary>
        /// View构造器
        /// </summary>
        IViewFactory viewFactory;

        public UIManager(IViewFactory viewFactory)
        {
            this.viewFactory = viewFactory;
            this.uiStack = new UIStack();
            this.commandQueue = new ViewCommandQueue();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize()
        {
            EnsureViewFactory();
        }

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="builder"></param>
        public void SetViewFactory(IViewFactory builder)
        {
            this.viewFactory = builder;
        }

        /// <summary>
        /// 通过默认的ViewModel打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <param name="param">打开时的参数</param>
        public void Open(string viewName, object param = null)
        {
            OpenView(new OpenViewCommand(new UIOpenParam(viewName, viewFactory, param: param)));
        }

        /// <summary>
        /// 通过默认的ViewModel异步打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        public async Task OpenAsync(string viewName, object param = null)
        {
            OpenView(new OpenViewCommand(new UIOpenParam(viewName, viewFactory, param: param, isAsync: true)));
            await WaitingForAllCommandExecuteComplete();
        }

        /// <summary>
        /// 通过指定的ViewModel打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            OpenView(new OpenViewCommand(new UIOpenParam(viewName, viewFactory, typeof(TViewModel), param : param)));
        }

        /// <summary>
        /// 通过指定的ViewModel异步打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async Task OpenAsync<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            OpenView(new OpenViewCommand(new UIOpenParam(viewName, viewFactory, typeof(TViewModel), param: param, isAsync: true)));
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
            OpenView(new OpenViewCommand(new UIOpenParam(viewName, viewFactory, typeof(TViewModel), typeof(TBehavior), param)));
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来异步打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async Task OpenAsync<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            OpenView(new OpenViewCommand(new UIOpenParam(viewName, viewFactory, typeof(TViewModel), typeof(TBehavior), param, true)));
            await WaitingForAllCommandExecuteComplete();
        }

        void OpenView(OpenViewCommand command)
        {
            EnsureViewFactory();
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

            command.Execute(uiStack);
            commandQueue.Enqueue(command);
            OnUpdate();
        }

        /// <summary>
        /// 将某个View置顶
        /// </summary>
        /// <param name="viewName"></param>
        public void Topping(string viewName)
        {
            var entity = uiStack.Topping(viewName);
            var viewConfig = ViewConfigCache.Get(entity.ViewModel);
            var layer = viewConfig == null ? (int)Layer.Common : viewConfig.layer;
            entity.SetLayer(layer);
            entity.SetOrder(uiStack.Count == 0 ? 0 : uiStack.Peek().View.Order + 1);

            //如果是全屏界面则使得背后的所有界面都不可见
            if (viewConfig != null && viewConfig.viewType == ViewType.FullScreen)
            {
                for (int i = uiStack.Count - 1; i >= 0; i--)
                {
                    var view = uiStack[i];
                    if (view.View.Layer <= entity.View.Layer)
                    {
                        view.Disable();
                    }
                }
            }
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
            command.Complete(uiStack);
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
            var entity = uiStack.GetUIEntity(viewName);
            if(entity == null)
            {
                return;
            }

            commandQueue.Enqueue(new CloseViewCommand(viewName));
            OnUpdate();
        }

        /// <summary>
        /// 关闭所有界面
        /// </summary>
        public void CloseAll()
        {
            //如果有正在打开中的界面 则取消打开
            foreach (var command in commandQueue.Commands)
            {
                if (command is OpenViewCommand)
                {
                    command.Cancel();
                    commandQueue.Remove(command);
                }
            }

            while(uiStack.Count > 0)
            {
                var entity = uiStack.Pop();
                entity.Destroy();
            }
        }

        void EnsureViewFactory()
        {
            if(viewFactory == null)
            {
                throw new System.Exception($"{nameof(viewFactory)} is null, please call {nameof(SetViewFactory)} first");
            }
        }

        /// <summary>
        /// 返回上一个界面
        /// </summary>
        public void Back()
        {
            var peek = uiStack.Peek();
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
