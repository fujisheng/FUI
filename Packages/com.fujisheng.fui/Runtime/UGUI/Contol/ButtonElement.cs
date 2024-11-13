using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Button))]
    public class ButtonElement : UGUIView
    {
        Button button;

        public class ClickedEventArgs : CommandArgs { public ClickedEventArgs(object sender) : base(sender) { } }

        /// <summary>
        /// ����ص�
        /// </summary>
        public Command<ClickedEventArgs> OnClick { get; private set; }

        /// <summary>
        /// �ı�Ԫ�� �����ֻ����
        /// </summary>
        public IReadOnlyBindableProperty<TextElement> TextElement { get; private set; } 

        /// <summary>
        /// �ı�����
        /// </summary>
        public BindableProperty<string> TextValue { get; private set; }

        /// <summary>
        /// ͼƬԪ�� �����ֻ����
        /// </summary>
        public IReadOnlyBindableProperty<ImageElement> ImageElement { get; private set; }

        /// <summary>
        /// ͼƬ����
        /// </summary>
        public BindableProperty<Sprite> ImageSprite { get; private set; }

        /// <summary>
        /// ͼƬ��Դ
        /// </summary>
        public BindableProperty<string> ImageSpriteSources { get; private set; }

        protected override void Initialize()
        {
            button = GetComponent<Button>();
            OnClick = new Command<ClickedEventArgs>();
            TextElement = new BindableProperty<TextElement>(button.GetComponentInChildren<TextElement>());
            TextValue = new BindableProperty<string>(TextElement.Value?.Text?.Value);
            ImageElement = new BindableProperty<ImageElement>(button.GetComponent<ImageElement>());
            ImageSprite = new BindableProperty<Sprite>(ImageElement.Value?.Sprite?.Value);
            ImageSpriteSources = new BindableProperty<string>(ImageElement.Value?.SpriteSources?.Value);

            button.onClick.AddListener(OnButtonClick);
            TextValue.OnValueChanged += OnSetTextValue;
            ImageSprite.OnValueChanged += OnSetImageSprite;
            ImageSpriteSources.OnValueChanged += OnSetImageSpriteSources;
        }

        void OnButtonClick()
        {
            OnClick?.Invoke(new ClickedEventArgs(this));
        }

        void OnSetTextValue(string oldValue, string newValue)
        {
            if(TextElement.Value == null)
            {
                return;
            }

            TextElement.Value.Text.Value = newValue;
        }

        void OnSetImageSprite(Sprite oldValue, Sprite newValue)
        {
            if(ImageElement.Value == null)
            {
                return;
            }

            ImageElement.Value.Sprite.Value = newValue;
        }

        void OnSetImageSpriteSources(string oldValue, string newValue)
        {
            if(ImageElement.Value == null)
            {
                return;
            }

            ImageElement.Value.SpriteSources.Value = newValue;
        }

        protected override void OnSetInteractable(bool oldInteractable, bool interactable)
        {
            button.interactable = interactable;
        }

        protected override void Destroy()
        {
            button.onClick.RemoveAllListeners();
            OnClick.ClearListeners();

            TextValue.Dispose();
            ImageSprite.Dispose();
            ImageSpriteSources.Dispose();
        }
    }
}