using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Button))]
    public class ButtonElement : SelectableElement<Button>
    {
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
        public BindableProperty<string> ImageSpriteSource { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();

            OnClick = new Command<ClickedEventArgs>();
            TextElement = new BindableProperty<TextElement>(Component.GetComponentInChildren<TextElement>());
            TextValue = new BindableProperty<string>(TextElement.Value?.Text?.Value);
            ImageElement = new BindableProperty<ImageElement>(Component.GetComponent<ImageElement>());
            ImageSprite = new BindableProperty<Sprite>(ImageElement.Value?.Sprite?.Value);
            ImageSpriteSource = new BindableProperty<string>(ImageElement.Value?.SpriteSource?.Value);

            Component.onClick.AddListener(OnButtonClick);
            TextValue.OnValueChanged += OnSetTextValue;
            ImageSprite.OnValueChanged += OnSetImageSprite;
            ImageSpriteSource.OnValueChanged += OnSetImageSpriteSources;
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

            ImageElement.Value.SpriteSource.Value = newValue;
        }

        protected override void Destroy()
        {
            base.Destroy();

            Component.onClick.RemoveAllListeners();
            OnClick.ClearListeners();

            TextValue.Dispose();
            ImageSprite.Dispose();
            ImageSpriteSource.Dispose();
        }
    }
}