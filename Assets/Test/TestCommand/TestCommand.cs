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
        [Binding("toggleValue", nameof(TextElement.TextObject))]
        [Binding("toggle", nameof(ToggleElement.IsOn), bindingMode: BindingMode.TwoWay)]
        public bool ToggleValue { get; set; } = true;




        [Command("toggle", nameof(ToggleElement.OnValueChanged))]
        public event Action<bool> ToggleValueChangedAction;



        [Command("toggle", nameof(ToggleElement.OnValueChanged))]
        public void OnToggleChanged(bool args)
        {
            Debug.Log($"OnToggleValueChangedByMethodCommand  {args}");
        }
    }

    public class TestCommandViewBehavior : ViewBehavior<TestCommandViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.ToggleValueChangedAction += OnToggleChanged;
        }

        void OnToggleChanged(bool isOn)
        {
            Debug.Log($"OnToggleValueChangedByEventCommand  {isOn}");
        }

        protected override void OnClose()
        {
            VM.ToggleValueChangedAction -= OnToggleChanged;
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
