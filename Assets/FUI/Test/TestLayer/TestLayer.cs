using FUI;
using FUI.Manager;
using FUI.Test;
using FUI.UGUI.Control;

using System;

using UnityEngine;

namespace Test.Layer
{
    [DefaultViewConfig(FUI.Manager.Layer.Background, ViewFlag.FullScreen)]
    [Binding("TestBackgroundView")]
    public class BackgroundViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "BackGroundView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick(ButtonElement.ClickedEventArgs args)
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestCommonView");
        }
    }

    [DefaultViewConfig(FUI.Manager.Layer.Common, ViewFlag.FullScreen)]
    [Binding("TestCommonView")]
    public class CommonViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "CommonView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick(ButtonElement.ClickedEventArgs args)
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestForegroundView");
        }
    }

    [DefaultViewConfig(FUI.Manager.Layer.Foreground, ViewFlag.FullScreen)]
    [Binding("TestForegroundView")]
    public class ForegroundViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "ForgroundView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick(ButtonElement.ClickedEventArgs args)
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestTopView");
        }
    }

    [DefaultViewConfig(FUI.Manager.Layer.Top, ViewFlag.FullScreen)]
    [Binding("TestTopView")]
    public class TopViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "TopView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick(ButtonElement.ClickedEventArgs args)
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestPopupView1");
        }
    }

    [DefaultViewConfig(FUI.Manager.Layer.Common)]
    [Binding("TestPopupView1")]
    public class PopupViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Title { get; set; } = "PopupView";

        [Command("btn_action", nameof(ButtonElement.OnClick))]
        public void OnClick(ButtonElement.ClickedEventArgs args)
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