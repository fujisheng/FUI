using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Manager
{
    /// <summary>
    /// 界面创建参数
    /// </summary>
    internal struct UIOpenTaskParam
    {
        /// <summary>
        /// 界面名字
        /// </summary>
        internal readonly string viewName;

        /// <summary>
        /// 指定的ViewModel类型
        /// </summary>
        internal readonly Type viewModelType;

        /// <summary>
        /// 指定的Presenter类型
        /// </summary>
        internal readonly Type presenterType;

        /// <summary>
        /// 打开时传入的参数
        /// </summary>
        internal readonly object param;

        /// <summary>
        /// 界面工厂
        /// </summary>
        internal readonly IViewFactory factory;

        /// <summary>
        /// 是否是异步创建
        /// </summary>
        internal readonly bool isAsync;

        /// <summary>
        /// 父级UI实体 被这个界面依赖的
        /// </summary>
        internal readonly UIEntity parent;

        internal UIOpenTaskParam(string viewName, IViewFactory factory, Type viewModelType = null, Type presenterType = null, object param = null, bool isAsync = false, UIEntity parent = null)
        {
            this.viewName = viewName;
            this.factory = factory;
            this.viewModelType = viewModelType;
            this.presenterType = presenterType;
            this.param = param;
            this.isAsync = isAsync;
            this.parent = parent;
        }
    }

    class OpenViewTask : ViewTask
    {
        readonly UIOpenTaskParam param;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly UIStack uiStack;
        readonly UIManager manager;
        // 进入动画是否已开始
        bool enterStarted;
        // 进入动画的取消标记
        CancellationTokenSource enterCancellation;

        internal OpenViewTask(UIManager manager, UIOpenTaskParam param, UIStack uiStack) : base(param.viewName)
        {
            this.manager = manager;
            this.param = param;
            this.uiStack = uiStack;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        internal async override void Execute()
        {
            if (param.isAsync)
            {
                result = await UIEntity.CreateAsync(param.viewName, param.factory, param.viewModelType, param.presenterType, cancellationTokenSource.Token);
            }
            else
            {
                result = UIEntity.Create(param.viewName, param.factory, param.viewModelType, param.presenterType);
            }

            if(result == null)
            {
                Logger.Instance.LogWarning($"open view:{param.viewName} failed");
            }

            isComplated = true;
            Logger.Instance.Log($"打开界面：{param.viewName}");
        }

        internal override bool TryComplete()
        {
            if (!isComplated)
            {
                return false;
            }

            if(result == null)
            {
                return true;
            }

            var viewConfig = UISettingsResolver.Get(result.ViewModel);
            SetLayer(uiStack, result, viewConfig);
            uiStack.Push(result, param.parent?.Name, param.param);
            OnComplete(uiStack, result, viewConfig);
            result.Interactable = false;
            manager.Enable(result, param.param);

            //过渡
            var transition = viewConfig.enterTransitionProvider?.Get(result.View);
            var completed = BeginTransition(
                transition,
                ref enterStarted,
                ref enterCancellation,
                onFinally: () =>
                {
                    result.Interactable = true;
                    cancellationTokenSource?.Dispose();
                    enterCancellation?.Dispose();
                    Logger.Instance.Log($"打开界面完成：{param.viewName}");
                },
                onNoTransition: () =>
                {
                    result.Interactable = true;
                    cancellationTokenSource?.Dispose();
                    Logger.Instance.Log($"打开界面完成：{param.viewName}");
                }
            );

            return true;
        }

        internal override void Cancel()
        {
            Logger.Instance.Log($"取消打开界面：{viewName}");
            if (!isComplated)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            if (enterStarted)
            {
                var viewConfig = UISettingsResolver.Get(result.ViewModel);
                var transition = viewConfig.enterTransitionProvider?.Get(result.View);
                CancelTransition(transition, ref enterCancellation);
            }

            result?.Destroy();
            cancellationTokenSource?.Dispose();
            enterCancellation?.Dispose();
        }

        /// <summary>
        /// 设置层级
        /// </summary>
        /// <param name="stack">当前ui栈</param>
        /// <param name="entity">当前ui实体</param>
        void SetLayer(UIStack stack, UIEntity entity, UISettings settings)
        {
            entity.Layer = settings.layer;
            entity.Order = stack.Count == 0 ? 0 : stack.Peek().Value.Entity.Order + 1;
        }

        void OnComplete(UIStack stack, UIEntity entity, UISettings settings)
        {
            //如果是全屏界面则使得背后的所有界面都不可见
            if (settings.flag.HasFlag(Attributes.FullScreen))
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    var view = stack[i];
                    if(view.Entity == entity)
                    {
                        continue;
                    }

                    if (view.Entity.Layer <= entity.Layer 
                        && view.Entity.Order < entity.Order 
                        && view.Entity.State.HasFlag(UIEntityState.Enabled))
                    {
                        manager.Disable(view.Entity);
                    }
                }
            }
        }
    }
}