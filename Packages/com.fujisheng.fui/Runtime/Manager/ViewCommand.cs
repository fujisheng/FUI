using System;
using System.Collections.Generic;
using System.Threading;

namespace FUI.Manager
{
    /// <summary>
    /// 界面命令队列
    /// </summary>
    class ViewCommandQueue
    {
        readonly List<ViewCommand> commands;

        /// <summary>
        /// 所有的命令
        /// </summary>
        internal IEnumerable<ViewCommand> Commands => commands;

        /// <summary>
        /// 初始化一个队列
        /// </summary>
        internal ViewCommandQueue()
        {
            commands = new List<ViewCommand>();
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="command">入队的命令</param>
        internal void Enqueue(ViewCommand command)
        {
            commands.Add(command);
        }

        /// <summary>
        /// 出队
        /// </summary>
        /// <returns>出队的命令</returns>
        internal ViewCommand Dequeue()
        {
            if (commands.Count == 0)
            {
                return null;
            }
            var command = commands[0];
            commands.RemoveAt(0);
            return command;
        }

        /// <summary>
        /// 获取最前面的命令
        /// </summary>
        /// <returns></returns>
        internal ViewCommand Peek()
        {
            if (commands.Count == 0)
            {
                return null;
            }
            return commands[0];
        }

        /// <summary>
        /// 移除一个命令
        /// </summary>
        /// <param name="command"></param>
        internal void Remove(ViewCommand command)
        {
            commands.Remove(command);
        }

        /// <summary>
        /// 清空所有命令
        /// </summary>
        internal void Clear()
        {
            commands.Clear();
        }

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => commands.Count;

        /// <summary>
        /// 是否存在某个命令
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal bool Exist(string viewName)
        {
            return commands.Exists(c => c.ViewName == viewName);
        }
    }

    /// <summary>
    /// 界面命令
    /// </summary>
    abstract class ViewCommand
    {
        internal abstract string ViewName { get; set; }
        internal abstract bool IsCompleted { get; set; }
        internal abstract UIEntity Result { get; set; }
        internal abstract void Execute(UIStack viewStack);
        internal abstract void Complete(UIStack viewStack);
        internal abstract void Cancel();
    }

    /// <summary>
    /// 界面创建参数
    /// </summary>
    internal struct UIOpenParam
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

        internal UIOpenParam(string viewName, IViewFactory factory, Type viewModelType = null, Type viewBehaviorType = null, object param = null, bool isAsync = false)
        {
            this.viewName = viewName;
            this.factory = factory;
            this.viewModelType = viewModelType;
            this.viewBehaviorType = viewBehaviorType;
            this.param = param;
            this.isAsync = isAsync;
        }
    }

    class OpenViewCommand : ViewCommand
    {
        readonly UIOpenParam param;
        readonly CancellationTokenSource cancellationTokenSource;

        internal override string ViewName { get; set; }
        internal override UIEntity Result { get; set; }
        internal override bool IsCompleted { get; set; }

        internal OpenViewCommand(UIOpenParam param)
        {
            this.ViewName = param.viewName;
            this.param = param;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.IsCompleted = false;
        }

        internal async override void Execute(UIStack viewStack)
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

        internal override void Complete(UIStack viewStack)
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
            SetLayer(viewStack, Result, viewConfig);
            Result.Enable(param.param);
            OnComplete(viewStack, Result, viewConfig);
            viewStack.Push(Result);
            cancellationTokenSource?.Dispose();
            UnityEngine.Debug.Log($"打开界面完成：{ViewName}");
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
        void SetLayer(UIStack stack, UIEntity entity, DefaultViewConfigAttribute viewConfig)
        {
            var layer = viewConfig == null ? (int)Layer.Common : viewConfig.layer;
            entity.SetLayer(layer);
            entity.SetOrder(stack.Count == 0 ? 0 : stack.Peek().View.Order + 1);
        }

        void OnComplete(UIStack stack, UIEntity entity, DefaultViewConfigAttribute viewConfig)
        {
            if(viewConfig == null)
            {
                return;
            }

            //如果是全屏界面则使得背后的所有界面都不可见
            if(viewConfig.viewType == ViewType.FullScreen)
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    var view = stack[i];
                    if(view.View.Layer <= entity.View.Layer)
                    {
                        view.Disable();
                    }
                }
            }
        }
    }

    class CloseViewCommand : ViewCommand
    {
        internal override string ViewName { get; set; }
        internal override bool IsCompleted { get; set; }
        internal override UIEntity Result { get; set; }
        internal CloseViewCommand(string viewName)
        {
            this.ViewName = viewName;
        }

        internal override void Execute(UIStack viewStack)
        {
            Result = viewStack.GetUIEntity(ViewName);
            IsCompleted = true;
        }

        internal override void Cancel()
        {
            Result = null;
            UnityEngine.Debug.Log($"取消关闭界面：{ViewName}");
        }

        internal override void Complete(UIStack viewStack)
        {
            if (Result == null)
            {
                return;
            }

            OnComplete(viewStack, Result, ViewConfigCache.Get(Result.ViewModel));
            Result.Disable();
            viewStack.Remove(Result);
            UnityEngine.Debug.Log($"关闭界面完成：{ViewName}");
            //TODO是否缓存被关闭的界面
        }

        void OnComplete(UIStack viewStack, UIEntity entity, DefaultViewConfigAttribute viewConfig)
        {
            if(viewConfig == null)
            {
                return;
            }

            //如果是全屏界面则使得背后的所有界面都可见直到遇到下一个全屏界面
            if(viewConfig.viewType == ViewType.FullScreen)
            {
                for (int i = viewStack.Count - 1; i >= 0; i--)
                {
                    
                    var view = viewStack[i];
                    if(view == entity)
                    {
                        continue;
                    }

                    if(view.View.Layer <= entity.View.Layer)
                    {
                        view.Enable();
                    }

                    var cfg = ViewConfigCache.Get(view.ViewModel);
                    if(cfg == null)
                    {
                        continue;
                    }

                    if(cfg.viewType == ViewType.FullScreen)
                    {
                        break;
                    }
                }
            }
        }
    }
}
