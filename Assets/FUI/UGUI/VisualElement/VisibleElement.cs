namespace FUI.UGUI.VisualElement
{
    public class VisibleElement : UGUIVisualElement<bool>
    {
        public override void UpdateValue(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}
