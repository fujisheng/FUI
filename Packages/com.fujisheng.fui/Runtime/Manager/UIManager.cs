using FUI.Bindable;

using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace FUI.Manager
{
    /// <summary>
    /// UI管理器  TODO：重构
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
        readonly ViewTaskQueue taskQueue;

        /// <summary>
        /// View构造器
        /// </summary>
        IViewFactory viewFactory;

        /// <summary>
        /// 所有打开的UI实体
        /// </summary>
        //public IReadOnlyList<UIEntity> OpeningEntities => uiStack.Items;

        public UIManager(IViewFactory viewFactory)
        {
            this.viewFactory = viewFactory;
            this.uiStack = new UIStack();
            this.taskQueue = new ViewTaskQueue();
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
            OpenView(new UIOpenTaskParam(viewName, viewFactory, param: param));
        }

        /// <summary>
        /// 通过默认的ViewModel异步打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        public async ValueTask OpenAsync(string viewName, object param = null)
        {
            OpenView(new UIOpenTaskParam(viewName, viewFactory, param: param, isAsync: true));
            await WaitingForAllTaskComplete();
        }

        /// <summary>
        /// 通过指定的ViewModel打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            OpenView(new UIOpenTaskParam(viewName, viewFactory, typeof(TViewModel), param : param));
        }

        /// <summary>
        /// 通过指定的ViewModel异步打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async ValueTask OpenAsync<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            OpenView(new UIOpenTaskParam(viewName, viewFactory, typeof(TViewModel), param: param, isAsync: true));
            await WaitingForAllTaskComplete();
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            OpenView(new UIOpenTaskParam(viewName, viewFactory, typeof(TViewModel), typeof(TBehavior), param));
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来异步打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async ValueTask OpenAsync<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            OpenView(new UIOpenTaskParam(viewName, viewFactory, typeof(TViewModel), typeof(TBehavior), param, true));
            await WaitingForAllTaskComplete();
        }

        void OpenView(UIOpenTaskParam param)
        {
            EnsureViewFactory();
            var viewConfig = UISettingsResolver.GetDefault(param.viewName);

            foreach (var item in taskQueue.Tasks)
            {
                if(item.viewName != param.viewName)
                {
                    continue;
                }

                //不允许多个实例的界面 已经在打开中了 直接返回
                if((item is OpenViewTask) && viewConfig != null && !viewConfig.Value.flag.HasFlag(Attributes.AllowMultiple))
                {
                    return;
                }

                //已经在关闭中了 取消关闭 并直接打开已有的
                if((item is CloseViewTask) && item.viewName == param.viewName)
                {
                    Topping(param.viewName);
                    item.Cancel();
                    taskQueue.Remove(item);
                    return;
                }
            }

            //要打开的界面已经被打开了 且标记为不允许多个实例 则直接置顶
            if(viewConfig!= null && !viewConfig.Value.flag.HasFlag(Attributes.AllowMultiple) && uiStack.GetUIEntity(param.viewName)!=null)
            {
                Topping(param.viewName);
                return;
            }

            //否则执行打开界面的任务
            taskQueue.Execute(new OpenViewTask(this, param, uiStack));
        }

        /// <summary>
        /// 将某个View置顶
        /// </summary>
        /// <param name="viewName"></param>
        public void Topping(string viewName)
        {
            var entity = uiStack.GetUIEntity(viewName);
            if(entity.State != UIEntityState.Enabled)
            {
                Enable(entity);
            }

            //已经在顶层了
            if (uiStack.Count != 0 && uiStack.Peek().Value.Entity == entity)
            {
                return;
            }

            var viewConfig = UISettingsResolver.Get(entity.ViewModel);
            if (uiStack.Topping(entity, out var lastPeek))
            {
                entity.Layer = viewConfig.layer;
                entity.Order = lastPeek == null ? 0 : lastPeek.Order + 1;
            }
            
            //如果是全屏界面则使得背后的所有界面都不可见
            if (viewConfig.flag.HasFlag(Attributes.FullScreen))
            {
                for (int i = uiStack.Count - 1; i >= 0; i--)
                {
                    var view = uiStack[i];
                    if (view.ParentViewName != entity.Name 
                        && view.Entity.Layer <= entity.Layer 
                        && view.Entity.Order < entity.Order
                        && view.Entity.State.HasFlag(UIEntityState.Enabled))
                    {
                        Disable(view.Entity);
                    }
                }
            }
        }

        /// <summary>
        /// 激活一个界面
        /// </summary>
        /// <param name="entity"></param>
        internal void Enable(UIEntity entity, object param = null)
        {
            var viewConfig = UISettingsResolver.Get(entity.ViewModel);
            if (viewConfig.preDependency != null)
            {
                foreach (var before in viewConfig.preDependency)
                {
                    OpenView(new UIOpenTaskParam(before, viewFactory, parent: entity));
                }
            }
            entity.Enable(param);
            if (viewConfig.postDependency != null)
            {
                foreach (var after in viewConfig.postDependency)
                {
                    OpenView(new UIOpenTaskParam(after, viewFactory, parent: entity));
                }
            }
        }

        /// <summary>
        /// 禁用一个界面
        /// </summary>
        /// <param name="entity"></param>
        internal void Disable(UIEntity entity)
        {
            var viewConfig = UISettingsResolver.Get(entity.ViewModel);
            //如果有前置依赖的界面则先关闭前置依赖的界面
            if (viewConfig.preDependency != null)
            {
                foreach (var before in viewConfig.preDependency)
                {
                    Close(before);
                }
            }

            //如果有后置依赖关闭后置依赖的界面
            if (viewConfig.postDependency != null)
            {
                foreach (var after in viewConfig.postDependency)
                {
                    Close(after);
                }
            }

            entity.Disable();
        }

        /// <summary>
        /// 关闭一个界面
        /// </summary>
        public void Close(string viewName)
        {
            //如果要关闭的这个界面正在打开中 则直接取消要打开的那个界面
            foreach (var task in taskQueue.Tasks)
            {
                if ((task is OpenViewTask) && task.viewName == viewName)
                {
                    task.Cancel();
                    taskQueue.Remove(task);
                    return;
                }
            }

            //要关闭的界面没有被打开
            var entity = uiStack.GetUIEntity(viewName);
            if(entity == null)
            {
                return;
            }

            taskQueue.Execute(new CloseViewTask(this, viewName, uiStack));
        }

        /// <summary>
        /// 关闭所有界面
        /// </summary>
        public void CloseAll()
        {
            //如果有正在打开中的界面 则取消打开
            var toRemove = taskQueue.Tasks.OfType<OpenViewTask>().ToList();
            foreach (var task in toRemove)
            {
                task.Cancel();
                taskQueue.Remove(task);
            }

            while (uiStack.Count > 0)
            {
                var stackInfo = uiStack.Pop();
                stackInfo.Value.Entity.Destroy();
            }
        }

        /// <summary>
        /// 确保ViewFactory不为空
        /// </summary>
        /// <exception cref="System.Exception"></exception>
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
            if(uiStack.Count == 0)
            {
                return;
            }

            for(int i = uiStack.Count - 1; i >= 0; i--)
            {
                var current = uiStack[i];
                //被依赖的界面不关闭
                if (current.ParentViewName == null)
                {
                    Close(current.Entity.Name);
                    break;
                }
            }
        }

        /// <summary>
        /// 等待所有命令执行完毕
        /// </summary>
        /// <returns></returns>
        public async ValueTask WaitingForAllTaskComplete()
        {
            if (taskQueue.Count == 0)
            {
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            var isCompleted = false;
            var lockObj = new object();

            void OnTaskQueueChanged()
            {
                lock (lockObj)
                {
                    if (taskQueue.Count == 0 && !isCompleted)
                    {
                        isCompleted = true;
                        taskQueue.OnCollectionChanged -= OnTaskQueueChanged;
                        tcs.TrySetResult(true);
                    }
                }
            }

            try
            {
                taskQueue.OnCollectionChanged += OnTaskQueueChanged;
                await tcs.Task;
            }
            finally
            {
                taskQueue.OnCollectionChanged -= OnTaskQueueChanged;
            }
        }
    }
}
