using System;
using System.Collections.Generic;
using System.Threading;

namespace FUI.Manager
{
    /// <summary>
    /// 界面任务队列
    /// </summary>
    class ViewTaskQueue
    {
        readonly List<ViewTask> tasks;

        /// <summary>
        /// 所有的任务
        /// </summary>
        internal IEnumerable<ViewTask> Tasks => tasks;

        /// <summary>
        /// 初始化一个队列
        /// </summary>
        internal ViewTaskQueue()
        {
            tasks = new List<ViewTask>();
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="task">入队的任务</param>
        internal void Execute(ViewTask task)
        {
            task.Execute();
            tasks.Add(task);
        }

        /// <summary>
        /// 出队
        /// </summary>
        /// <returns>出队的任务</returns>
        internal ViewTask Complete()
        {
            if (tasks.Count == 0)
            {
                return null;
            }
            var task = tasks[0];
            tasks.RemoveAt(0);
            task.Complete();
            return task;
        }

        /// <summary>
        /// 获取最前面的任务
        /// </summary>
        /// <returns></returns>
        internal ViewTask Peek()
        {
            if (tasks.Count == 0)
            {
                return null;
            }
            return tasks[0];
        }

        /// <summary>
        /// 移除一个任务
        /// </summary>
        /// <param name="task"></param>
        internal void Remove(ViewTask task)
        {
            tasks.Remove(task);
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        internal void Clear()
        {
            tasks.Clear();
        }

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => tasks.Count;

        /// <summary>
        /// 是否存在某个任务
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal bool Exist(string viewName)
        {
            return tasks.Exists(c => c.ViewName == viewName);
        }
    }

    /// <summary>
    /// 界面任务
    /// </summary>
    abstract class ViewTask
    {
        internal abstract string ViewName { get; set; }
        internal abstract bool IsCompleted { get; set; }
        internal abstract UIEntity Result { get; set; }
        internal abstract void Execute();
        internal abstract void Complete();
        internal abstract void Cancel();
    }

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

            var viewConfig = ViewConfigCache.Get(Result.ViewModel);
            SetLayer(uiStack, Result, viewConfig);
            Result.Enable(param.param);
            OnComplete(uiStack, Result, viewConfig);
            uiStack.Push(Result);
            cancellationTokenSource?.Dispose();

        }

        internal override void Cancel()
        {
            UnityEngine.Debug.Log($"取消打开界面：{ViewName}");
            if (!IsCompleted)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            Result?.Destroy();
            cancellationTokenSource?.Dispose();
        }
        
        /// <summary>
        /// 设置层级
        /// </summary>
        /// <param name="stack">当前ui栈</param>
        /// <param name="entity">当前ui实体</param>
        void SetLayer(UIStack stack, UIEntity entity, ViewConfig viewConfig)
        {
            entity.Layer = viewConfig.layer;
            entity.Order = stack.Count == 0 ? 0 : stack.Peek().Order + 1;
        }

        void OnComplete(UIStack stack, UIEntity entity, ViewConfig viewConfig)
        {
            //如果是全屏界面则使得背后的所有界面都不可见
            if(viewConfig.flag.HasFlag(ViewFlag.FullScreen))
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    var view = stack[i];
                    if(view.Layer <= entity.Layer)
                    {
                        view.Disable();
                    }
                }
            }
        }
    }

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

            OnComplete(uiStack, Result, ViewConfigCache.Get(Result.ViewModel));
            Result.Destroy();
            uiStack.Remove(Result);
            UnityEngine.Debug.Log($"关闭界面完成：{ViewName}");
            //TODO是否缓存被关闭的界面
        }

        void OnComplete(UIStack viewStack, UIEntity entity, ViewConfig viewConfig)
        {
            //如果是全屏界面则使得背后的所有界面都可见直到遇到下一个全屏界面
            if(viewConfig.flag.HasFlag(ViewFlag.FullScreen))
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

                    var cfg = ViewConfigCache.Get(view.ViewModel);

                    if(cfg.flag.HasFlag(ViewFlag.FullScreen))
                    {
                        break;
                    }
                }
            }
        }
    }
}
