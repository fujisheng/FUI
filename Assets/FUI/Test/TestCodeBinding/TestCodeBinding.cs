using FUI;
using FUI.Test;
using FUI.UGUI.VisualElement;

using System.Collections.Generic;

using UnityEngine;

namespace Test.CodeBinding
{
    [Binding("TestCodeBindingView")]
    public class TestCodeBindingViewModel : ViewModel
    {
        [Binding("txt_int", typeof(IntToStringConverter), typeof(TextElement))]
        public int TestInt { get; set; }

        [Binding("txt_string", elementType: typeof(TextElement), bindingType:BindingType.OneWay)]
        public string TestString { get; set; }

        [Binding("txt_float", converterType: typeof(FloatToStringConverter))]
        public float TestFloat { get; set; }

        [Binding("txt_bool")]
        public bool TestBool { get; set; }
    }

    public class TestCodeBindingViewBehavior : ViewBehavior<TestCodeBindingViewModel>
    {
        protected override void OnOpen(object param)
        {
            UnityEngine.Debug.Log("OnOpen  TestCodeBindingView");
            VM.TestInt = 1;
            VM.TestString = "Hello, code binding";
            VM.TestFloat = 3.14f;
            VM.TestBool = true;
        }
    }

    public class TestCodeBinding : MonoBehaviour
    {
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestCodeBindingView");
        }
    }
}
