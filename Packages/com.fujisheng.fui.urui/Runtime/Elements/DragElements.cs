using FUI.Binding;
using FUI.Rendering;
using Element = global::URUI.Elements.Element;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 拖拽容器；原生事件逻辑由共享行为持有。
    /// </summary>
    public sealed class DragElement : ElementAdapter, IPointerDragElement, IPointerDragCancellationElement
    {
        readonly DragBehavior drag;
        public DragElement(Element element, string name, float threshold = 4) : base(element, name)
        {
            drag = DragBehavior.Create(element, base.Dispose, threshold);
        }
        public Command<PointerPosition> DragStarted => drag.Started;
        public Command<PointerPosition> DragMoved => drag.Moved;
        public Command<PointerPosition> DragEnded => drag.Ended;
        public Command<PointerPosition> DragCancelled => drag.Cancelled;
        public override void Dispose()
        {
            try
            {
                drag.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }

    /// <summary>
    /// 保留图片能力的拖拽元素。
    /// </summary>
    public sealed class DragImageElement : ImageElement, IPointerDragElement, IPointerDragCancellationElement
    {
        readonly DragBehavior drag;
        public DragImageElement(Element element, string name, float threshold = 4) : base(element, name)
        {
            drag = DragBehavior.Create(element, base.Dispose, threshold);
        }
        public Command<PointerPosition> DragStarted => drag.Started;
        public Command<PointerPosition> DragMoved => drag.Moved;
        public Command<PointerPosition> DragEnded => drag.Ended;
        public Command<PointerPosition> DragCancelled => drag.Cancelled;
        public override void Dispose()
        {
            try
            {
                drag.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }

    /// <summary>
    /// 保留文本能力的拖拽元素。
    /// </summary>
    public sealed class DragTextElement : TextElement, IPointerDragElement, IPointerDragCancellationElement
    {
        readonly DragBehavior drag;
        public DragTextElement(Element element, string name, float threshold = 4) : base(element, name)
        {
            drag = DragBehavior.Create(element, base.Dispose, threshold);
        }
        public Command<PointerPosition> DragStarted => drag.Started;
        public Command<PointerPosition> DragMoved => drag.Moved;
        public Command<PointerPosition> DragEnded => drag.Ended;
        public Command<PointerPosition> DragCancelled => drag.Cancelled;
        public override void Dispose()
        {
            try
            {
                drag.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }

    /// <summary>
    /// 保留原生键盘按钮行为的拖拽元素。
    /// </summary>
    public sealed class DragButtonElement : ButtonElement, IPointerDragElement, IPointerDragCancellationElement
    {
        readonly DragBehavior drag;
        public DragButtonElement(Element element, string name, float threshold = 4) : base(element, name)
        {
            drag = DragBehavior.Create(element, base.Dispose, threshold);
        }
        public Command<PointerPosition> DragStarted => drag.Started;
        public Command<PointerPosition> DragMoved => drag.Moved;
        public Command<PointerPosition> DragEnded => drag.Ended;
        public Command<PointerPosition> DragCancelled => drag.Cancelled;
        public override void Dispose()
        {
            try
            {
                drag.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
