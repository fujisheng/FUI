using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Generic 
{ 
    public class TextElement : IElement<string>
    {
        public readonly Text text;
        public TextElement(Text text)
        {
            this.text = text;
        }

        public void UpdateValue(string value)
        {
            this.text.text = value;
        }
    }

    public class ColorElement : IElement<Color>
    {
        public readonly Graphic graphic;
        public ColorElement(Graphic image)
        {
            this.graphic = image;
        }

        public void UpdateValue(Color value)
        {
            this.graphic.color = value;
        }
    }

    public class VisibleElement : IElement<bool>
    {
        public readonly GameObject gameObject;
        public VisibleElement(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        public void UpdateValue(bool value)
        {
            this.gameObject.SetActive(value);
        }
    }

    public class PositionElement : IElement<Vector2>
    {
        public readonly RectTransform rectTransform;
        public PositionElement(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
        }

        public void UpdateValue(Vector2 value)
        {
            this.rectTransform.anchoredPosition = value;
        }
    }
}