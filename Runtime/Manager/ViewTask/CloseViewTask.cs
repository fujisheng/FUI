using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Manager
{
    class CloseViewTask : ViewTask
    {
        // 管理器实例
        UIManager manager;
        // UI栈实例
        readonly UIStack uiStack;
        // 退出动画是否已开始
        bool exitStarted;
        // 退出动画的取消标记
        CancellationTokenSource exitCancellation;

        internal CloseViewTask(UIManager manager, string viewName, UIStack uiStack) : base(viewName)
        {
            this.manager = manager;
            this.uiStack = uiStack;
        }

        internal override void Execute()
        {
            result = uiStack.GetUIEntity(viewName);
            Logger.Instance.Log($"关闭界面：{viewName}");
            isComplated = true;
        }

        internal override void Cancel()
        {
            // 取消关闭任务
            if (exitStarted)
            {
                var viewConfig = UISettingsResolver.Get(result.ViewModel);
                var transition = viewConfig.exitTransitionProvider?.Get(result.View);
                CancelTransition(transition, ref exitCancellation);
            }

            result = null;
            Logger.Instance.Log($"取消关闭界面：{viewName}");
            exitCancellation?.Dispose();
        }

        internal override bool TryComplete()
        {
            if (!isComplated)
            {
                return false;
            }

            if (result == null)
            {
                return true;
            }

            // 在播放退出动画期间禁止交互
            result.Interactable = false;

            var viewConfig = UISettingsResolver.Get(result.ViewModel);
            var transition = viewConfig.exitTransitionProvider?.Get(result.View);
            var completed = BeginTransition(
                transition,
                ref exitStarted,
                ref exitCancellation,
                onFinally: () =>
                {
                    try
                    {
                        if (result != null)
                        {
                            manager.Disable(result);
                            uiStack.Remove(result);
                            OnComplete(uiStack, result, UISettingsResolver.Get(result.ViewModel));
                            result.Interactable = true;
                            Logger.Instance.Log($"关闭界面完成：{viewName}");
                            result.Destroy();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.LogException(e);
                    }
                    finally
                    {
                        try { exitCancellation?.Dispose(); } catch { }
                    }
                },
                onNoTransition: () =>
                {
                    manager.Disable(result);
                    uiStack.Remove(result);
                    OnComplete(uiStack, result, UISettingsResolver.Get(result.ViewModel));
                    result.Interactable = true;
                    Logger.Instance.Log($"关闭界面完成：{viewName}");
                    result.Destroy();
                }
            );

            return completed;
        }

        void OnComplete(UIStack viewStack, UIEntity entity, UISettings settings)
        {
            //如果是全屏界面则使得背后的所有界面都可见直到遇到下一个全屏界面
            if(settings.flag.HasFlag(Attributes.FullScreen))
            {
                for (int i = viewStack.Count - 1; i >= 0; i--)
                {
                    var view = viewStack[i];
                    if(view.Entity == entity)
                    {
                        continue;
                    }

                    if(view.Entity.Layer <= entity.Layer)
                    {
                        manager.Enable(view.Entity);
                    }

                    var cfg = UISettingsResolver.Get(view.Entity.ViewModel);

                    if(cfg.flag.HasFlag(Attributes.FullScreen))
                    {
                        break;
                    }
                }
            }
        }
    }
}
