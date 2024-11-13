using FUI;
using FUI.Test;
using FUI.UGUI.Control;

using UnityEngine;

namespace Test.Command
{
    [Binding("TestElementView")]
    public class TestElementViewModel : ViewModel
    {
        [Binding("TestText", nameof(TextElement.Text))]
        public string Text { get; set; } = "TextValue";

        [Binding("TestImage", nameof(ImageElement.SpriteSource))]
        [Binding("TestImageSrc", nameof(TextElement.Text))]
        public string Image { get; set; } = "testIcon1";

        [Binding("TestRawImage", nameof(RawImageElement.TextureSource))]
        [Binding("TestRawImageSrc", nameof(TextElement.Text))]
        public string Texture { get; set; } = "testIcon2";

        [Binding("TestDropdownValue", nameof(TextElement.TextObject))]
        [Binding("TestDropdown", nameof(DropdownElement.Value), bindingMode:BindingMode.TwoWay)]
        public int Dropdown { get; set; } = 0;
        [Command("TestDropdown", nameof(DropdownElement.OnValueChanged))]
        public void OnDropdownChanged(DropdownElement.ValueChangedArgs args)
        {
            Debug.Log($"OnDropdownChanged  {args.Sender}  {args.Value}");
        }

        [Binding("TestInputValue", nameof(TextElement.Text))]
        [Binding("TestInput", nameof(InputFieldElement.Text), bindingMode:BindingMode.TwoWay)]
        public string Input { get; set; } = "";
        [Command("TestInput", nameof(InputFieldElement.OnValueChanged))]
        public void OnInputChanged(InputFieldElement.ValueChangedArgs args)
        {
            Debug.Log($"OnInputChanged  {args.Sender}  {args.Value}");
        }

        [Binding("TestSliderValue", nameof(TextElement.TextObject))]
        [Binding("TestSlider", nameof(SliderElement.Value), bindingMode:BindingMode.TwoWay)]
        public float Slider { get; set; } = 0.5f;
        [Command("TestSlider", nameof(SliderElement.OnValueChanged))]
        public void OnSliderChanged(SliderElement.ValueChangedArgs args)
        {
            Debug.Log($"OnSliderChanged  {args.Sender}  {args.Value}");
        }

        [Binding("TestScrollbarValue", nameof(TextElement.TextObject))]
        [Binding("TestScrollbar", nameof(ScrollbarElement.Value), bindingMode: BindingMode.TwoWay)]
        public float Scrollbar { get; set; } = 0.5f;
        [Command("TestScrollbar", nameof(ScrollbarElement.OnValueChanged))]
        public void OnScrollbarChanged(ScrollbarElement.ValueChangedArgs args)
        {
            Debug.Log($"OnScrollbarChanged  {args.Sender}  {args.Value}");
        }

        [Binding("TestToggleValue", nameof(TextElement.TextObject))]
        [Binding("TestToggle", nameof(ToggleElement.IsOn), bindingMode: BindingMode.TwoWay)]
        public bool ToggleValue { get; set; } = true;
        [Command("TestToggle", nameof(ToggleElement.OnValueChanged))]
        public void OnToggleChanged(ToggleElement.ValueChangedArgs args)
        {
            Debug.Log($"OnToggleValueChanged  {args.Sender}  {args.IsOn}");
        }

        [Binding("TestButtonCount", nameof(TextElement.TextObject))]
        public int ButtonClickCount { get; set; } = 0;
        [Command("TestButton", nameof(ButtonElement.OnClick))]
        public void OnTestButtonClick(ButtonElement.ClickedEventArgs args)
        {
            Debug.Log($"OnButtonClick:{args.Sender}");
            ButtonClickCount++;
        }
    }

    public class TestElementViewBehavior : ViewBehavior<TestElementViewModel>
    {
        protected override void OnOpen(object param)
        {
           
        }
    }

    public class TestElement : MonoBehaviour
    {
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestElementView");
        }
    }
}
