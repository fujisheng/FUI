namespace FUI.Manager
{
    class CloseViewTask : ViewTask
    {
        readonly UIStack uiStack;
        internal override string ViewName { get; set; }
        internal override bool IsCompleted { get; set; }
        internal override UIEntity Result { get; set; }

        internal CloseViewTask(string viewName, UIStack uiStack)
        {
            this.ViewName = viewName;
            this.uiStack = uiStack;
        }

        internal override void Execute()
        {
            Result = uiStack.GetUIEntity(ViewName);
            IsCompleted = true;
            UnityEngine.Debug.Log($"关闭界面：{ViewName}");
        }

        internal override void Cancel()
        {
            Result = null;
            UnityEngine.Debug.Log($"取消关闭界面：{ViewName}");
        }

        internal override void Complete()
        {
            if (Result == null)
            {
                return;
            }

            OnComplete(uiStack, Result, UIConfigResolver.Get(Result.ViewModel));
            Result.Destroy();
            uiStack.Remove(Result);
            UnityEngine.Debug.Log($"关闭界面完成：{ViewName}");
            //TODO是否缓存被关闭的界面
        }

        void OnComplete(UIStack viewStack, UIEntity entity, UIConfig viewConfig)
        {
            //如果是全屏界面则使得背后的所有界面都可见直到遇到下一个全屏界面
            if(viewConfig.flag.HasFlag(Attributes.FullScreen))
            {
                for (int i = viewStack.Count - 1; i >= 0; i--)
                {
                    var view = viewStack[i];
                    if(view == entity)
                    {
                        continue;
                    }

                    if(view.Layer <= entity.Layer)
                    {
                        view.Enable();
                    }

                    var cfg = UIConfigResolver.Get(view.ViewModel);

                    if(cfg.flag.HasFlag(Attributes.FullScreen))
                    {
                        break;
                    }
                }
            }
        }
    }
}
