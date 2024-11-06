using FUI;
using FUI.Test;
using FUI.UGUI.Control;

using System;

using UnityEngine;

namespace Test.Command
{
    [Binding("TestCommandView")]
    public class TestCommandViewModel : ViewModel
    {
        [Binding("toggle", nameof(ToggleElement.IsOn), bindingType:BindingType.TwoWay)]
        public bool ToggleValue { get; set; }

        [Command("toggle", nameof(ToggleElement.OnValueChanged))]
        void OnToggleChanged(bool value)
        {
            Console.WriteLine($"OnToggleValueChanged:{value}");
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
