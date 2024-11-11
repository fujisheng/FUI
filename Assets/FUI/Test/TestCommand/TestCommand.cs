using FUI;
using FUI.Test;
using FUI.UGUI.Control;

using UnityEngine;

namespace Test.Command
{
    [Binding("TestCommandView")]
    public class TestCommandViewModel : ViewModel
    {
        [Binding("toggleValue", nameof(TextElement.Text), typeof(BoolToStringConverter))]
        [Binding("toggle", nameof(ToggleElement.IsOn), bindingType: BindingType.TwoWay)]
        public bool ToggleValue { get; set; } = true;

        [Command("toggle", nameof(ToggleElement.OnValueChanged))]
        void OnToggleChanged(ToggleElement.ValueChangedArgs args)
        {
            Debug.Log($"OnToggleValueChanged  {args.Sender}  {args.IsOn}");
        }
    }

    public class TestCommandViewBehavior : ViewBehavior<TestCommandViewModel>
    {
        protected override void OnOpen(object param)
        {
           
        }
    }

    public class TestCommand : MonoBehaviour
    {
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestCommandView");
        }
    }
}
