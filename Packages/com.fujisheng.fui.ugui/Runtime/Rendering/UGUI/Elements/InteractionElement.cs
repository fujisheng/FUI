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
    public class InteractionElement : RectTransformElement, IPointerDragElement, IPointerDragCancellationElement, IPointerContactElement,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
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

        public Command<PointerPosition> DragStarted { get; private set; }
        public Command<PointerPosition> DragMoved { get; private set; }
        public Command<PointerPosition> DragEnded { get; private set; }
        public Command<PointerPosition> DragCancelled { get; private set; }
        public Command<PointerContact> PointerPressed {get;private set;}
        public Command<PointerContact> PointerMoved {get;private set;}
        public Command<PointerContact> PointerReleased {get;private set;}
        public Command<PointerContact> PointerCancelled {get;private set;}
        PointerEventData contactData;
        EventSystem contactSystem;
        BaseInputModule contactModule;
        int? contactPointer;
        PointerContact lastContact;
        int? draggingPointer;
        PointerPosition lastDragPosition;
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
            PointerPressed = Track(new Command<PointerContact>());
            PointerMoved = Track(new Command<PointerContact>());
            PointerReleased = Track(new Command<PointerContact>());
            PointerCancelled = Track(new Command<PointerContact>());
            TrackCleanup(CancelContact);
            OnPointerEnter = Track(new Command<PointerEventData>());
            OnPointerExit = Track(new Command<PointerEventData>());
            OnPointerDown = Track(new Command<PointerEventData>());
            OnPointerUp = Track(new Command<PointerEventData>());
            OnPointerClick = Track(new Command<PointerEventData>());
            OnBeginDrag = Track(new Command<PointerEventData>());
            DragStarted = Track(new Command<PointerPosition>());
            DragMoved = Track(new Command<PointerPosition>());
            DragEnded = Track(new Command<PointerPosition>());
            DragCancelled = Track(new Command<PointerPosition>());
            OnDrag = Track(new Command<PointerEventData>());
            OnEndDrag = Track(new Command<PointerEventData>());
            OnDrop = Track(new Command<PointerEventData>());
            OnScroll = Track(new Command<PointerEventData>());
            OnSelect = Track(new Command<BaseEventData>());
            OnDeselect = Track(new Command<BaseEventData>());
            OnMove = Track(new Command<AxisEventData>());
            OnSubmit = Track(new Command<BaseEventData>());
            OnCancel = Track(new Command<BaseEventData>());
            TrackCleanup(CancelDrag);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => OnPointerEnter?.Invoke(eventData);
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnPointerExit?.Invoke(eventData);
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            OnPointerDown?.Invoke(eventData);
            if(!isActiveAndEnabled || contactPointer.HasValue || eventData.button != PointerEventData.InputButton.Left) {return;}
            contactPointer=eventData.pointerId;contactData=eventData;contactSystem=EventSystem.current;contactModule=contactSystem?contactSystem.currentInputModule:null; lastContact=ReadContact(eventData); PointerPressed?.Invoke(lastContact);
        }
        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            OnPointerUp?.Invoke(eventData);
            if(contactPointer != eventData.pointerId) {return;}
            contactPointer=null;contactData=null;contactModule=null;contactSystem=null; lastContact=ReadContact(eventData); PointerReleased?.Invoke(lastContact);
        }
        PointerContact ReadContact(PointerEventData data)
        {
            var plane=transform.parent as RectTransform;
            if(!plane) {throw new MissingReferenceException("Pointer contact requires a RectTransform parent plane.");}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(plane,data.position,data.pressEventCamera,out var local);
            return new PointerContact(data.pointerId,new PointerPosition(data.position.x,data.position.y),new PointerPosition(local.x,local.y));
        }
        void CancelContact()
        {
            if(!contactPointer.HasValue) {return;} contactPointer=null;contactData=null;contactModule=null;contactSystem=null; PointerCancelled?.Invoke(lastContact);
        }
        void IPointerMoveHandler.OnPointerMove(PointerEventData eventData)=>MoveContact(eventData);
        void MoveContact(PointerEventData eventData)
        {
            if(contactPointer!=eventData.pointerId || (lastContact.screen.x==eventData.position.x && lastContact.screen.y==eventData.position.y)){return;}
            lastContact=ReadContact(eventData);PointerMoved?.Invoke(lastContact);
        }
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => OnPointerClick?.Invoke(eventData);
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (draggingPointer.HasValue && draggingPointer != eventData.pointerId) { OnBeginDrag?.Invoke(eventData); return; }
            draggingPointer = eventData.pointerId;
            lastDragPosition = new PointerPosition(eventData.position.x, eventData.position.y);
            OnBeginDrag?.Invoke(eventData);
            if (draggingPointer == eventData.pointerId) { DragStarted?.Invoke(lastDragPosition); }
        }
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            OnDrag?.Invoke(eventData);
            MoveContact(eventData);
            if (draggingPointer != eventData.pointerId) { return; }
            lastDragPosition = new PointerPosition(eventData.position.x, eventData.position.y);
            DragMoved?.Invoke(lastDragPosition);
        }
        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            var completed = draggingPointer == eventData.pointerId;
            if (completed) { draggingPointer = null; }
            OnEndDrag?.Invoke(eventData);
            if (completed) { DragEnded?.Invoke(new PointerPosition(eventData.position.x, eventData.position.y)); }
        }
        void IDropHandler.OnDrop(PointerEventData eventData) => OnDrop?.Invoke(eventData);
        void IScrollHandler.OnScroll(PointerEventData eventData) => OnScroll?.Invoke(eventData);
        void ISelectHandler.OnSelect(BaseEventData eventData) => OnSelect?.Invoke(eventData);
        void IDeselectHandler.OnDeselect(BaseEventData eventData) => OnDeselect?.Invoke(eventData);
        void IMoveHandler.OnMove(AxisEventData eventData) => OnMove?.Invoke(eventData);
        void ISubmitHandler.OnSubmit(BaseEventData eventData) => OnSubmit?.Invoke(eventData);
        void ICancelHandler.OnCancel(BaseEventData eventData) { CancelContact(); CancelDrag(); OnCancel?.Invoke(eventData); }

        // EventSystem持续更新同一按压数据；退出hover链且尚未跨drag阈值时仍跟踪它。
        void LateUpdate()
        {
            if(!contactPointer.HasValue||contactData==null){return;}
            if(contactSystem!=null&&(!contactSystem.isActiveAndEnabled||contactModule!=null&&(!contactModule.isActiveAndEnabled||!ReferenceEquals(contactSystem.currentInputModule,contactModule))))
            {CancelContact();return;}
            MoveContact(contactData);
        }
        void OnApplicationFocus(bool focused){if(!focused){CancelContact();CancelDrag();}}
        void OnDisable() {CancelContact(); CancelDrag();}

        void CancelDrag()
        {
            if (!draggingPointer.HasValue) { return; }
            draggingPointer = null;
            DragCancelled?.Invoke(lastDragPosition);
        }
    }
}
