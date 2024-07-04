using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(Image))]
    public class ImageElement : UGUIVisualElement<Sprite>
    {
        Image image;

        protected override void Initialize()
        {
            image = GetComponent<Image>();
        }

        public override void UpdateValue(Sprite value)
        {
            image.sprite = value;
        }
    }
}
