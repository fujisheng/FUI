﻿using FUI;
using FUI.Test;
using FUI.UGUI.Control;

using System.Collections.Generic;

using UnityEngine;

namespace Test.CodeBinding
{
    [Binding("TestCodeBindingView")]
    public class TestCodeBindingViewModel : ViewModel
    {
        [Binding("txt_int", nameof(TextElement.TextObject))]
        public int TestInt { get; set; }

        [Binding("txt_string", nameof(TextElement.Text), bindingType:BindingType.OneWay)]
        public string TestString { get; set; }

        [Binding("txt_float", nameof(TextElement.Text), typeof(FloatToStringConverter))]
        public float TestFloat { get; set; }

        [Binding("txt_bool", nameof(TextElement.Text), typeof(BoolToStringConverter))]
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
