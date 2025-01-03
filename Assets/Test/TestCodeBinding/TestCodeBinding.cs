using FUI;
using FUI.Test;
using FUI.UGUI.Control;

using UnityEngine;

namespace Test.CodeBinding
{
    [Binding("TestCodeBindingView")]
    public class TestCodeBindingViewModel : ViewModel
    {
        [Binding("txt_int", nameof(TextElement.TextObject))]
        public int TestInt { get; set; }

        [Binding("txt_string", nameof(TextElement.Text), bindingMode:BindingMode.OneWay)]
        public string TestString { get; set; }

        [Binding("txt_float", nameof(TextElement.Text), typeof(FloatToStringConverter))]
        public float TestFloat { get; set; }

        [Binding("txt_bool", nameof(TextElement.Text), typeof(BoolToStringConverter))]
        public bool TestBool { get; set; }

        //下面调用来查看堆栈是否正确输出
        public void OnClick()
        {
            UnityEngine.Debug.Log($"{this}");
        }
    }

    public class TestCodeBindingViewBehavior : ViewBehavior<TestCodeBindingViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.OnClick();
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
