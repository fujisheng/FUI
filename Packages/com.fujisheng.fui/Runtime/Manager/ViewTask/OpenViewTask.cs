using System;
using System.Threading;

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
        /// 指定的ViewBehavior类型
        /// </summary>
        internal readonly Type viewBehaviorType;

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

        internal UIOpenTaskParam(string viewName, IViewFactory factory, Type viewModelType = null, Type viewBehaviorType = null, object param = null, bool isAsync = false, UIEntity parent = null)
        {
            this.viewName = viewName;
            this.factory = factory;
            this.viewModelType = viewModelType;
            this.viewBehaviorType = viewBehaviorType;
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
                result = await UIEntity.CreateAsync(param.viewName, param.factory, param.viewBehaviorType, param.viewBehaviorType, cancellationTokenSource.Token);
            }
            else
            {
                result = UIEntity.Create(param.viewName, param.factory, param.viewBehaviorType, param.viewBehaviorType);
            }

            if(result == null)
            {
                UnityEngine.Debug.LogWarning($"open view:{param.viewName} failed");
            }

            isComplated = true;
            OnComplated?.Invoke();
            UnityEngine.Debug.Log($"打开界面：{param.viewName}");
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
            uiStack.Push(result, param.parent?.Name);
            OnComplete(uiStack, result, viewConfig);
            manager.Enable(result, param.param);
            cancellationTokenSource?.Dispose();
            UnityEngine.Debug.Log($"打开界面完成：{param.viewName}");
            //UnityEngine.Debug.Log(uiStack);
            return true;
        }

        internal override void Cancel()
        {
            UnityEngine.Debug.Log($"取消打开界面：{viewName}");
            if (!isComplated)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            result?.Destroy();
            cancellationTokenSource?.Dispose();
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