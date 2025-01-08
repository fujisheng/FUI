using FUI;
using FUI.Manager;
using FUI.Test;
using FUI.UGUI.Control;

using System;

using UnityEngine;

namespace Test.Layer
{
    [Config(FUI.Manager.Layer.Background, Attributes.FullScreen)]
    [Binding("TestBackgroundView")]
    public class BackgroundViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "BackGroundView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestCommonView");
        }
    }

    [Config(FUI.Manager.Layer.Common, Attributes.FullScreen)]
    [Binding("TestCommonView")]
    public class CommonViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "CommonView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestForegroundView");
        }
    }

    [Config(FUI.Manager.Layer.Foreground, Attributes.FullScreen)]
    [Binding("TestForegroundView")]
    public class ForegroundViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "ForgroundView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestTopView");
        }
    }

    [Config(FUI.Manager.Layer.Top, Attributes.FullScreen)]
    [Binding("TestTopView")]
    public class TopViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "TopView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestPopupView1");
        }
    }

    [Config(FUI.Manager.Layer.Common)]
    [Binding("TestPopupView1")]
    public class PopupViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "PopupView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick()
        {
            TestLauncher.Instance.UIManager.Close("TestPopupView1");
        }
    }

    public class TestLayer : MonoBehaviour
    {
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestBackgroundView");
        }
    }

}