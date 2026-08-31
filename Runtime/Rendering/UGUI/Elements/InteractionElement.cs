using FUI.Binding;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 Unity EventSystem 的指针、拖拽和导航事件公开为类型安全的 FUI Command。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("FUI/Elements/Interaction")]
    public class InteractionElement : RectTransformElement,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler,
        ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
    {
        /// <summary>指针进入事件。</summary>
        public Command<PointerEventData> OnPointerEnter { get; private set; }

        /// <summary>指针离开事件。</summary>
        public Command<PointerEventData> OnPointerExit { get; private set; }

        /// <summary>指针按下事件。</summary>
        public Command<PointerEventData> OnPointerDown { get; private set; }

        /// <summary>指针抬起事件。</summary>
        public Command<PointerEventData> OnPointerUp { get; private set; }

        /// <summary>指针点击事件。</summary>
        public Command<PointerEventData> OnPointerClick { get; private set; }

        /// <summary>开始拖拽事件。</summary>
        public Command<PointerEventData> OnBeginDrag { get; private set; }

        /// <summary>拖拽事件。</summary>
        public Command<PointerEventData> OnDrag { get; private set; }

        /// <summary>结束拖拽事件。</summary>
        public Command<PointerEventData> OnEndDrag { get; private set; }

        /// <summary>放置事件。</summary>
        public Command<PointerEventData> OnDrop { get; private set; }

        /// <summary>滚轮事件。</summary>
        public Command<PointerEventData> OnScroll { get; private set; }

        /// <summary>获得选择事件。</summary>
        public Command<BaseEventData> OnSelect { get; private set; }

        /// <summary>失去选择事件。</summary>
        public Command<BaseEventData> OnDeselect { get; private set; }

        /// <summary>导航移动事件。</summary>
        public Command<AxisEventData> OnMove { get; private set; }

        /// <summary>提交事件。</summary>
        public Command<BaseEventData> OnSubmit { get; private set; }

        /// <summary>取消事件。</summary>
        public Command<BaseEventData> OnCancel { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            OnPointerEnter = Track(new Command<PointerEventData>());
            OnPointerExit = Track(new Command<PointerEventData>());
            OnPointerDown = Track(new Command<PointerEventData>());
            OnPointerUp = Track(new Command<PointerEventData>());
            OnPointerClick = Track(new Command<PointerEventData>());
            OnBeginDrag = Track(new Command<PointerEventData>());
            OnDrag = Track(new Command<PointerEventData>());
            OnEndDrag = Track(new Command<PointerEventData>());
            OnDrop = Track(new Command<PointerEventData>());
            OnScroll = Track(new Command<PointerEventData>());
            OnSelect = Track(new Command<BaseEventData>());
            OnDeselect = Track(new Command<BaseEventData>());
            OnMove = Track(new Command<AxisEventData>());
            OnSubmit = Track(new Command<BaseEventData>());
            OnCancel = Track(new Command<BaseEventData>());
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => OnPointerEnter?.Invoke(eventData);
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnPointerExit?.Invoke(eventData);
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) => OnPointerDown?.Invoke(eventData);
        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) => OnPointerUp?.Invoke(eventData);
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => OnPointerClick?.Invoke(eventData);
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) => OnBeginDrag?.Invoke(eventData);
        void IDragHandler.OnDrag(PointerEventData eventData) => OnDrag?.Invoke(eventData);
        void IEndDragHandler.OnEndDrag(PointerEventData eventData) => OnEndDrag?.Invoke(eventData);
        void IDropHandler.OnDrop(PointerEventData eventData) => OnDrop?.Invoke(eventData);
        void IScrollHandler.OnScroll(PointerEventData eventData) => OnScroll?.Invoke(eventData);
        void ISelectHandler.OnSelect(BaseEventData eventData) => OnSelect?.Invoke(eventData);
        void IDeselectHandler.OnDeselect(BaseEventData eventData) => OnDeselect?.Invoke(eventData);
        void IMoveHandler.OnMove(AxisEventData eventData) => OnMove?.Invoke(eventData);
        void ISubmitHandler.OnSubmit(BaseEventData eventData) => OnSubmit?.Invoke(eventData);
        void ICancelHandler.OnCancel(BaseEventData eventData) => OnCancel?.Invoke(eventData);
    }
}
