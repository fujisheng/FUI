using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Button))]
    public class ButtonElement : SelectableElement<Button>
    {
        /// <summary>
        /// 点击回调
        /// </summary>
        public Command OnClick { get; private set; }

        /// <summary>
        /// 文本元素 这个是只读的
        /// </summary>
        public IReadOnlyBindableProperty<TextElement> TextElement { get; private set; } 

        /// <summary>
        /// 文本内容
        /// </summary>
        public BindableProperty<string> TextValue { get; private set; }

        /// <summary>
        /// 图片元素 这个是只读的
        /// </summary>
        public IReadOnlyBindableProperty<ImageElement> ImageElement { get; private set; }

        /// <summary>
        /// 图片精灵
        /// </summary>
        public BindableProperty<Sprite> ImageSprite { get; private set; }

        /// <summary>
        /// 图片资源
        /// </summary>
        public BindableProperty<string> ImageSpriteSource { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            OnClick = new Command();
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
            OnClick?.Invoke();
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Component.onClick.RemoveAllListeners();
            OnClick.ClearListeners();

            TextValue.Dispose();
            ImageSprite.Dispose();
            ImageSpriteSource.Dispose();
        }
    }
}