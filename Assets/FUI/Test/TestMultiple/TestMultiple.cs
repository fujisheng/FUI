﻿using FUI;
using FUI.Manager;
using FUI.Test;
using FUI.UGUI.Control;

using System.Threading.Tasks;

using UnityEngine;

namespace Test.Multiple
{
    //可以取消掉AllowMultiple或者FullScreen标签，看看效果
    [DefaultViewConfig(FUI.Manager.Layer.Foreground, ViewFlag.FullScreen | ViewFlag.AllowMultiple)]
    [Binding("TestMultipleView")]
    public class TestMultipleViewModel : ViewModel
    {
        [Binding("txt_notify", nameof(TextElement.Text))]
        public string Notify { get; set; } = "Notify_1";
    }

    public class TestMultipleViewBehavior : ViewBehavior<TestMultipleViewModel>
    {
        protected override async void OnOpen(object param)
        {
            if (param is string str)
            {
                VM.Notify = str;
            }
        }
    }

    public class TestMultiple : MonoBehaviour
    {
        async void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestMultipleView", "exp +1");
            await Task.Delay(500);
            TestLauncher.Instance.UIManager.OpenAsync("TestMultipleView", "hp +10");
            await Task.Delay(500);
            TestLauncher.Instance.UIManager.OpenAsync("TestMultipleView", "exp +4");
            await Task.Delay(500);
            TestLauncher.Instance.UIManager.OpenAsync("TestMultipleView", "hp +1");
            await Task.Delay(500);
            TestLauncher.Instance.UIManager.OpenAsync("TestMultipleView", "hp +40");
        }
    }
}
