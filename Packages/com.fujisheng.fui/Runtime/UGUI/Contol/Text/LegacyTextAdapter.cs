using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Text))]
    public class LegacyTextAdapter : MonoBehaviour, IText
    {
        Text component;
        Text Component
        {
            get
            {
                if (component == null)
                {
                    component = GetComponent<Text>();
                }
                return component;
            }
        }

        public string Text
        {
            get => Component.text;
            set
            {
                Component.text = value;
            }
        }

        public int FontSize
        {
            get => Component.fontSize;
            set
            {
                Component.fontSize = value;
            }
        }

        public Color Color
        {
            get => Component.color;
            set
            {
                Component.color = value;
            }
        }
    }
}