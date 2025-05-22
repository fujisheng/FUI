using TMPro;

using UnityEngine;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPTextAdapter : MonoBehaviour, IText
    {
        TextMeshProUGUI component;
        TextMeshProUGUI Component
        {
            get
            {
                if (component == null)
                {
                    component = GetComponent<TextMeshProUGUI>();
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
            get => (int)Component.fontSize;
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