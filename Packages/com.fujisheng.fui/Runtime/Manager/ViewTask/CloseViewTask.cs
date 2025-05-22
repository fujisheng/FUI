namespace FUI.Manager
{
    class CloseViewTask : ViewTask
    {
        UIManager manager;
        readonly UIStack uiStack;

        internal CloseViewTask(UIManager manager, string viewName, UIStack uiStack) : base(viewName)
        {
            this.manager = manager;
            this.uiStack = uiStack;
        }

        internal override void Execute()
        {
            result = uiStack.GetUIEntity(viewName);
            UnityEngine.Debug.Log($"关闭界面：{viewName}");
            isComplated = true;
            OnComplated?.Invoke();
        }

        internal override void Cancel()
        {
            result = null;
            UnityEngine.Debug.Log($"取消关闭界面：{viewName}");
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

            manager.Disable(result);
            uiStack.Remove(result);
            OnComplete(uiStack, result, UISettingsResolver.Get(result.ViewModel));
            UnityEngine.Debug.Log($"关闭界面完成：{viewName}");
            result.Destroy();
            //TODO是否缓存被关闭的界面
            //UnityEngine.Debug.Log(uiStack);
            return true;
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
