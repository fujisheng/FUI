using System;
using System.Collections.Generic;
using System.Globalization;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using URUI.Events;
using Element = global::URUI.Elements.Element;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 共享拖拽状态机；保留点击，越界捕获，取消不冒充正常提交。
    /// </summary>
    internal sealed class DragBehavior : IDisposable
    {
        readonly Element element;
        readonly List<IDisposable> subscriptions = new List<IDisposable>();
        readonly float threshold;
        Element captureOwner;
        int pointer = -1;
        int suppressPointer = -1;
        Vector2 start;
        Vector2 last;
        bool dragging;
        bool disposed;
        internal Command<PointerPosition> Started
        {
            get;
        } = new Command<PointerPosition>();
        internal Command<PointerPosition> Moved
        {
            get;
        } = new Command<PointerPosition>();
        internal Command<PointerPosition> Ended
        {
            get;
        } = new Command<PointerPosition>();
        internal Command<PointerPosition> Cancelled
        {
            get;
        } = new Command<PointerPosition>();

        internal static DragBehavior Create(Element element, Action cleanup, float threshold = 4)
        {
            try
            {
                return new DragBehavior(element, threshold);
            }
            catch (Exception failure)
            {
                try
                {
                    cleanup();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }
        }

        DragBehavior(Element element, float threshold)
        {
            this.element = element;
            this.threshold = threshold;
            if (!float.IsFinite(threshold) || threshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(element), "Drag threshold must be finite and nonnegative.");
            }
            try
            {
                subscriptions.Add(element.RegisterCallback<PointerDownEvent>(OnDown));
                subscriptions.Add(element.RegisterCallback<PointerMoveEvent>(OnMove));
                subscriptions.Add(element.RegisterCallback<PointerUpEvent>(OnUp));
                subscriptions.Add(element.RegisterCallback<PointerCancelEvent>(evt => CancelPointer(evt.PointerId)));
                subscriptions.Add(element.RegisterCallback<PointerCaptureOutEvent>(evt => CancelPointer(evt.PointerId)));
                subscriptions.Add(element.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown));
            }
            catch
            {
                foreach (var subscription in subscriptions)
                {
                    subscription.Dispose();
                }
                throw;
            }
        }

        const global::URUI.Elements.ElementState InteractiveMask =
            global::URUI.Elements.ElementState.Valid |
            global::URUI.Elements.ElementState.Attached |
            global::URUI.Elements.ElementState.VisibleInHierarchy;

        bool IsInteractive()
        {
            var flags = element.StateFlags;
            return (flags & InteractiveMask) == InteractiveMask &&
                (flags & global::URUI.Elements.ElementState.DisabledInHierarchy) == 0;
        }

        static PointerPosition ScreenPosition(Vector2 value) => new PointerPosition(value.x, Screen.height - value.y);

        void OnDown(PointerDownEvent evt)
        {
            if (disposed || pointer >= 0 || evt.Button != 0 || !IsInteractive())
            {
                return;
            }
            pointer = evt.PointerId;
            start = last = evt.Position;
            dragging = false;
            suppressPointer = -1;
            captureOwner = evt.Target;
            // 捕获实际命中子节点，普通点击仍回到原按钮；父滚动不竞争此显式手势。
            captureOwner.CapturePointer(pointer);
            evt.StopPropagation();
        }

        void OnMove(PointerMoveEvent evt)
        {
            if (disposed || evt.PointerId != pointer || pointer < 0)
            {
                return;
            }
            if (!IsInteractive())
            {
                CancelPointer(pointer);
                return;
            }
            last = evt.Position;
            evt.StopPropagation();
            if (!dragging && (last - start).sqrMagnitude < threshold * threshold)
            {
                return;
            }
            evt.PreventDefault();
            var starting = !dragging;
            dragging = true;
            suppressPointer = pointer;
            var id = pointer;
            var position = ScreenPosition(last);
            // 与点击桥接保持一致：业务可同步克隆投影，必须先退出原生回调，仍在本次输入返回前执行。
            element.Document.RunAfterEvent(() =>
            {
                if (disposed || pointer != id || !dragging)
                {
                    return;
                }
                if (starting)
                {
                    Started.Invoke(position);
                }
                if (disposed || pointer != id || !dragging)
                {
                    return;
                }
                if (!IsInteractive())
                {
                    CancelPointer(id);
                    return;
                }
                Moved.Invoke(position);
            });
        }

        void OnUp(PointerUpEvent evt)
        {
            if (disposed || evt.PointerId != pointer || pointer < 0)
            {
                return;
            }
            if (!IsInteractive())
            {
                CancelPointer(pointer);
                return;
            }
            last = evt.Position;
            var completed = dragging;
            evt.StopPropagation();
            if (completed)
            {
                evt.PreventDefault();
            }
            ReleaseCapture();
            var position = ScreenPosition(last);
            if (completed)
            {
                element.Document.RunAfterEvent(() =>
                {
                    if (!disposed)
                    {
                        Ended.Invoke(position);
                    }
                });
            }
        }

        void OnClick(ClickEvent evt)
        {
            if (suppressPointer < 0 || evt.PointerId != suppressPointer)
            {
                return;
            }
            suppressPointer = -1;
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        void CancelPointer(int id)
        {
            if (pointer < 0 || pointer != id)
            {
                return;
            }
            var wasDragging = dragging;
            ReleaseCapture();
            var position = ScreenPosition(last);
            if (wasDragging)
            {
                element.Document.RunAfterEvent(() => Cancelled.Invoke(position));
            }
        }

        void ReleaseCapture()
        {
            var owner = captureOwner;
            var id = pointer;
            pointer = -1;
            dragging = false;
            captureOwner = null;
            if (owner != null && owner.IsValid)
            {
                owner.ReleasePointer(id);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            try
            {
                CancelPointer(pointer);
            }
            finally
            {
                foreach (var subscription in subscriptions)
                {
                    subscription.Dispose();
                }
                subscriptions.Clear();
                element.Document.RunAfterEvent(() =>
                {
                    Started.Dispose();
                    Moved.Dispose();
                    Ended.Dispose();
                    Cancelled.Dispose();
                });
            }
        }
    }
}
