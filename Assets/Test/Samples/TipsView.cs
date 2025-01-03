using FUI.Manager;
using FUI.Test;
using FUI.UGUI.Control;

using System;

namespace FUI.Samples
{
    [DefaultViewConfig(Layer.Foreground)]
    [Binding("TipsView")]
    public class TipsViewModel : ViewModel
    {
        [Binding("title", nameof(TextElement.Text))]
        public string Title { get; set; }

        [Binding("content", nameof(TextElement.Text))]
        public string Content { get; set; }

        [Command("closeButton", nameof(ButtonElement.OnClick))]
        public event Action OnClose;
    }

    public class TipsData
    {
        public readonly string title;
        public readonly string content;

        public TipsData(string title, string content)
        {
            this.title = title;
            this.content = content;
        }
    }

    public class TipsViewBehavior : ViewBehavior<TipsViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.OnClose += OnCloseButtonClick;
            UnityEngine.Debug.Log("OnOpen  TipsView");
            if(param is TipsData tips)
            {
                VM.Title = tips.title;
                VM.Content = tips.content;
            }
            else
            {
                VM.Title = string.Empty;
                VM.Content = string.Empty;
            }
        }

        void OnCloseButtonClick()
        {
            UnityEngine.Debug.Log("OnCloseButtonClick  TipsView");
            TestLauncher.Instance.UIManager.Back();
        }

        protected override void OnClose()
        {
            VM.OnClose -= OnCloseButtonClick;
        }
    }
}