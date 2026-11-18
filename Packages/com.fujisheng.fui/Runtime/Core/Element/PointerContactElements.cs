using FUI.Binding;

namespace FUI.Rendering
{
    /// <summary>指针屏幕位置与明确参考平面的本地位置；两者均以左下为原点、Y向上。</summary>
    public readonly struct PointerContact
    {
        public readonly int pointerId;
        public readonly PointerPosition screen;
        public readonly PointerPosition local;
        public PointerContact(int pointerId,PointerPosition screen,PointerPosition local)
        {this.pointerId=pointerId;this.screen=screen;this.local=local;}
    }
    /// <summary>单指针接触的按下、移动、释放及取消；释放与取消保持独立。</summary>
    public interface IPointerContactElement:IElement
    {
        Command<PointerContact> PointerPressed{get;}
        Command<PointerContact> PointerMoved{get;}
        Command<PointerContact> PointerReleased{get;}
        Command<PointerContact> PointerCancelled{get;}
    }
    /// <summary>把显示屏幕位置投影到元素所属UI平面，不携带引擎相机对象。</summary>
    public interface IScreenPositionElement:IElement
    {BindableProperty<PointerPosition> ScreenPositionValue{get;}}
    /// <summary>相对父显示节点的四元数旋转；与世界旋转契约分开。</summary>
    public interface ILocalQuaternionRotationElement:IElement
    {BindableProperty<RotationValue> LocalRotationValue{get;}}
}
