using System;
using System.Collections.Generic;
using FUI.Rendering.UGUI.Elements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI
{
    /// <summary>在当前 Canvas 的控件后提供一个只参与输入的页面命中。</summary>
    [AddComponentMenu("")]
    sealed class PageInputRaycaster : BaseRaycaster, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IScrollHandler
    {
        static readonly List<(PointerEventData Data, int Frame)> observedPointers = new();
        static int observedFrame = -1;
        static PointerEventData lastObserved;
        static int activeRaycasters;
        Transform page;
        Canvas canvas;
        GraphicRaycaster graphics;
        CanvasGroup group;
        bool authoredRaycasts;
        bool blocksInputBehind;

        public override Camera eventCamera => !canvas || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : graphics ? graphics.eventCamera : canvas.worldCamera;
        public override int sortOrderPriority => canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? canvas.sortingOrder : base.sortOrderPriority;
        public override int renderOrderPriority => canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.renderOrder : base.renderOrderPriority;

        internal void Configure(View view, bool blocksInputBehind)
        {
            page = view.transform;
            canvas = view.GetComponent<Canvas>();
            graphics = view.GetComponent<GraphicRaycaster>();
            if (!view.TryGetComponent(out group))
            {
                group = view.gameObject.AddComponent<CanvasGroup>();
            }
            authoredRaycasts = group.blocksRaycasts;
            this.blocksInputBehind = blocksInputBehind;
        }

        internal void SetInputEnabled(bool enabled)
        {
            group.blocksRaycasts = enabled && authoredRaycasts;
            if (!enabled)
            {
                CancelInput(false);
            }
        }

        internal void CancelCoveredInput()
        {
            if (blocksInputBehind && isActiveAndEnabled && canvas && canvas.isActiveAndEnabled)
            {
                CancelInput(true);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            activeRaycasters++;
        }

        protected override void OnDisable()
        {
            CancelInput(false);
            base.OnDisable();
            if (--activeRaycasters == 0)
            {
                observedPointers.Clear();
                lastObserved = null;
                observedFrame = -1;
            }
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
            Observe(eventData);
            if (!blocksInputBehind || !canvas || !canvas.isActiveAndEnabled)
            {
                return;
            }

            var camera = eventCamera;
            var display = camera ? camera.targetDisplay : canvas.targetDisplay;
            var position = eventData.position;
            var relative = Display.RelativeMouseAt(position);
            if (relative != Vector3.zero)
            {
                if ((int)relative.z != display)
                {
                    return;
                }
                position = relative;
            }
            else if (eventData.displayIndex != display)
            {
                return;
            }

            var screenSpace = canvas.renderMode != RenderMode.WorldSpace;
            if (screenSpace
                ? !(camera ? camera.pixelRect : canvas.pixelRect).Contains(position)
                : !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)canvas.transform, position, camera))
            {
                return;
            }

            // 与作者的 GraphicRaycaster 共享排序键，同一个 Canvas 内真实控件优先。
            var distance = 0f;
            if (camera)
            {
                var plane = new Plane(canvas.transform.forward, canvas.transform.position);
                if (!plane.Raycast(camera.ScreenPointToRay(position), out distance))
                {
                    return;
                }
            }
            results.Add(new RaycastResult
            {
                gameObject = gameObject,
                module = graphics ? graphics : this,
                depth = int.MinValue,
                distance = distance,
                screenPosition = position,
                displayIndex = display,
                sortingLayer = canvas.sortingLayerID,
                sortingOrder = canvas.sortingOrder,
                index = results.Count,
            });
        }

        internal static void Observe(PointerEventData data)
        {
            if (activeRaycasters == 0)
            {
                return;
            }

            var frame = Time.frameCount;
            if (observedFrame != frame)
            {
                for (var index = observedPointers.Count - 1; index >= 0; index--)
                {
                    var pointer = observedPointers[index];
                    if (pointer.Frame < frame - 1 && (!pointer.Data.rawPointerPress && !pointer.Data.pointerDrag
                        || !pointer.Data.currentInputModule || !pointer.Data.currentInputModule.isActiveAndEnabled))
                    {
                        observedPointers.RemoveAt(index);
                    }
                }
                observedFrame = frame;
                lastObserved = null;
            }
            if (ReferenceEquals(lastObserved, data))
            {
                return;
            }

            lastObserved = data;
            for (var index = 0; index < observedPointers.Count; index++)
            {
                if (ReferenceEquals(observedPointers[index].Data, data))
                {
                    observedPointers[index] = (data, frame);
                    return;
                }
            }
            observedPointers.Add((data, frame));
        }

        void CancelInput(bool covered)
        {
            if (!page)
            {
                return;
            }

            using var lease = ListPool<PointerEventData>.Get(out var cancelled);
            foreach (var pointer in observedPointers)
            {
                var data = pointer.Data;
                var target = data.rawPointerPress ? data.rawPointerPress : data.pointerDrag;
                if (!target && data.eligibleForClick)
                {
                    // 当前模块可能仍在分发 PointerDown，还没写回已按下的对象。
                    target = data.pointerPressRaycast.gameObject;
                }
                if (Contains(target, covered))
                {
                    cancelled.Add(data);
                }
            }
            foreach (var data in cancelled)
            {
                CancelPointer(data);
            }

            var system = EventSystem.current;
            if (system && !system.alreadySelecting && Contains(system.currentSelectedGameObject, covered))
            {
                system.SetSelectedGameObject(null);
            }
        }

        bool Contains(GameObject target, bool covered)
        {
            if (!target)
            {
                return false;
            }
            if (target.transform.IsChildOf(page))
            {
                return !covered;
            }
            if (!covered)
            {
                return false;
            }

            var other = target.GetComponentInParent<Canvas>();
            if (!other)
            {
                return false;
            }

            if (!other.overrideSorting)
            {
                other = other.rootCanvas;
            }
            if (canvas.renderMode != other.renderMode || canvas.targetDisplay != other.targetDisplay)
            {
                return false;
            }
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder != other.sortingOrder)
            {
                return canvas.sortingOrder > other.sortingOrder;
            }
            return canvas.sortingLayerID == other.sortingLayerID
                ? canvas.sortingOrder > other.sortingOrder
                : SortingLayer.GetLayerValueFromID(canvas.sortingLayerID) > SortingLayer.GetLayerValueFromID(other.sortingLayerID);
        }

        static void CancelPointer(PointerEventData data)
        {
            var press = data.pointerPress;
            var drag = data.pointerDrag;
            var dragging = data.dragging;
            data.eligibleForClick = false;
            data.pointerPress = null;
            data.rawPointerPress = null;
            data.pointerClick = null;
            data.pointerDrag = null;
            data.dragging = false;

            // 归还控件的内部状态，避免把取消解释为抬起、放置或点击命令。
            if (press && press.TryGetComponent<Selectable>(out var selectable))
            {
                selectable.OnPointerUp(data);
            }
            if (dragging && drag && drag.TryGetComponent<ScrollRect>(out var scroll))
            {
                scroll.OnEndDrag(data);
            }
            if (press && press.TryGetComponent<InteractionElement>(out var contact))
            {
                contact.CancelPointer(data.pointerId);
            }
            if (drag && drag != press && drag.TryGetComponent<InteractionElement>(out var gesture))
            {
                gesture.CancelPointer(data.pointerId);
            }
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) { }
        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) { }
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) { }
        void IScrollHandler.OnScroll(PointerEventData eventData) { }
    }
}
