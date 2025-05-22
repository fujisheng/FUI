using FUI.Bindable;

using UnityEngine;
using UnityEngine.EventSystems;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 事件触发器元素
    /// </summary>
    [RequireComponent(typeof(EventTrigger))]
    public class EventTriggerElement : UIElement<EventTrigger>
    {
        public Command<BaseEventData> OnPointerEnter { get; private set; }
        public Command<BaseEventData> OnPointerExit { get; private set; }
        public Command<BaseEventData> OnPointerDown { get; private set; }
        public Command<BaseEventData> OnPointerUp { get; private set; }
        public Command<BaseEventData> OnPointerClick { get; private set; }

        public Command<BaseEventData> OnBeginDrag { get; private set; }
        public Command<BaseEventData> OnDrag { get; private set; }
        public Command<BaseEventData> OnEndDrag { get; private set; }

        public Command<BaseEventData> OnDrop { get; private set; }
        public Command<BaseEventData> OnScroll { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            OnPointerEnter = new Command<BaseEventData>();
            OnPointerExit = new Command<BaseEventData>();
            OnPointerDown = new Command<BaseEventData>();
            OnPointerUp = new Command<BaseEventData>();
            OnPointerClick = new Command<BaseEventData>();

            OnBeginDrag = new Command<BaseEventData>();
            OnDrag = new Command<BaseEventData>();
            OnEndDrag = new Command<BaseEventData>();

            OnDrop = new Command<BaseEventData>();
            OnScroll = new Command<BaseEventData>();

            AddEventTrigger(EventTriggerType.PointerEnter, (data) => OnPointerEnter.Invoke(data));
            AddEventTrigger(EventTriggerType.PointerExit, (data) => OnPointerExit.Invoke(data));
            AddEventTrigger(EventTriggerType.PointerDown, (data) => OnPointerDown.Invoke(data));
            AddEventTrigger(EventTriggerType.PointerUp, (data) => OnPointerUp.Invoke(data));
            AddEventTrigger(EventTriggerType.PointerClick, (data) => OnPointerClick.Invoke(data));

            AddEventTrigger(EventTriggerType.BeginDrag, (data) => OnBeginDrag.Invoke(data));
            AddEventTrigger(EventTriggerType.Drag, (data) => OnDrag.Invoke(data));
            AddEventTrigger(EventTriggerType.EndDrag, (data) => OnEndDrag.Invoke(data));

            AddEventTrigger(EventTriggerType.Drop, (data) => OnDrop.Invoke(data));
            AddEventTrigger(EventTriggerType.Scroll, (data) => OnScroll.Invoke(data));
        }

        void AddEventTrigger(EventTriggerType eventType, System.Action<PointerEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener((data) => action((PointerEventData)data));
            Component.triggers.Add(entry);
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            OnPointerEnter.ClearListeners();
            OnPointerExit.ClearListeners();
            OnPointerDown.ClearListeners();
            OnPointerUp.ClearListeners();
            OnPointerClick.ClearListeners();

            OnBeginDrag.ClearListeners();
            OnDrag.ClearListeners();
            OnEndDrag.ClearListeners();

            OnDrop.ClearListeners();
            OnScroll.ClearListeners();

            Component.triggers.Clear();
        }
    }
}