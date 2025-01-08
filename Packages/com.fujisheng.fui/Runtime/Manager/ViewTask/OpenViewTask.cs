using System;
using System.Threading;

namespace FUI.Manager
{
    /// <summary>
    /// ���洴������
    /// </summary>
    internal struct UIOpenTaskParam
    {
        /// <summary>
        /// ��������
        /// </summary>
        internal readonly string viewName;

        /// <summary>
        /// ָ����ViewModel����
        /// </summary>
        internal readonly Type viewModelType;

        /// <summary>
        /// ָ����ViewBehavior����
        /// </summary>
        internal readonly Type viewBehaviorType;

        /// <summary>
        /// ��ʱ����Ĳ���
        /// </summary>
        internal readonly object param;

        /// <summary>
        /// ���湤��
        /// </summary>
        internal readonly IViewFactory factory;

        /// <summary>
        /// �Ƿ����첽����
        /// </summary>
        internal readonly bool isAsync;

        internal UIOpenTaskParam(string viewName, IViewFactory factory, Type viewModelType = null, Type viewBehaviorType = null, object param = null, bool isAsync = false)
        {
            this.viewName = viewName;
            this.factory = factory;
            this.viewModelType = viewModelType;
            this.viewBehaviorType = viewBehaviorType;
            this.param = param;
            this.isAsync = isAsync;
        }
    }

    class OpenViewTask : ViewTask
    {
        readonly UIOpenTaskParam param;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly UIStack uiStack;

        internal override string ViewName { get; set; }
        internal override UIEntity Result { get; set; }
        internal override bool IsCompleted { get; set; }

        internal OpenViewTask(UIOpenTaskParam param, UIStack uiStack)
        {
            this.ViewName = param.viewName;
            this.param = param;
            this.uiStack = uiStack;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.IsCompleted = false;
        }

        internal async override void Execute()
        {
            UIEntity entity;

            if (param.isAsync)
            {
                entity = await UIEntity.CreateAsync(param.viewName, param.factory, param.viewBehaviorType, param.viewBehaviorType, cancellationTokenSource.Token);
            }
            else
            {
                entity = UIEntity.Create(param.viewName, param.factory, param.viewBehaviorType, param.viewBehaviorType);
            }

            if (entity == null)
            {
                IsCompleted = true;
                UnityEngine.Debug.LogWarning($"open view:{param.viewName} failed");
                return;
            }
            Result = entity;
            IsCompleted = true;
        }

        internal override void Complete()
        {
            if (!IsCompleted)
            {
                return;
            }

            if (Result == null)
            {
                return;
            }

            var viewConfig = UIConfigResolver.Get(Result.ViewModel);
            SetLayer(uiStack, Result, viewConfig);
            Result.Enable(param.param);
            OnComplete(uiStack, Result, viewConfig);
            uiStack.Push(Result);
            cancellationTokenSource?.Dispose();

        }

        internal override void Cancel()
        {
            UnityEngine.Debug.Log($"ȡ���򿪽��棺{ViewName}");
            if (!IsCompleted)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            Result?.Destroy();
            cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// ���ò㼶
        /// </summary>
        /// <param name="stack">��ǰuiջ</param>
        /// <param name="entity">��ǰuiʵ��</param>
        void SetLayer(UIStack stack, UIEntity entity, UIConfig viewConfig)
        {
            entity.Layer = viewConfig.layer;
            entity.Order = stack.Count == 0 ? 0 : stack.Peek().Order + 1;
        }

        void OnComplete(UIStack stack, UIEntity entity, UIConfig viewConfig)
        {
            //�����ȫ��������ʹ�ñ�������н��涼���ɼ�
            if (viewConfig.flag.HasFlag(Attributes.FullScreen))
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    var view = stack[i];
                    if (view.Layer <= entity.Layer)
                    {
                        view.Disable();
                    }
                }
            }
        }
    }
}