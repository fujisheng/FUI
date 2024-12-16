using FUI;
using FUI.Test;

using UnityEngine;

namespace Test.Descriptor
{
    public class TestDescriptorViewModel : ViewModel
    {
        public int TestInt { get; set; }

        public string TestString { get; set; }

        public float TestFloat { get; set; }

        public bool TestBool { get; set; }

        //����������鿴��ջ�Ƿ���ȷ���
        public void OnClick()
        {
            UnityEngine.Debug.Log($"{this}");
        }
    }

    public class TestDescriptorViewBehavior : ViewBehavior<TestDescriptorViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.OnClick();
            UnityEngine.Debug.Log("OnOpen  TestDescriptorView");
            VM.TestInt = 1;
            VM.TestString = "Hello, code binding";
            VM.TestFloat = 3.14f;
            VM.TestBool = true;

            //�������䱨��  ���鿴�����ջ�Ƿ���ȷ��ʾ
            UnityEngine.Debug.Log(param.ToString());
        }
    }

    public class TestDescriptor : MonoBehaviour
    {
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestDescriptorView");
        }
    }
}
