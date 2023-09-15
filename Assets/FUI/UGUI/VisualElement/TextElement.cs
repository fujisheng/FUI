using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(Text))]
    public class TextElement : UGUIVisualElement<string>
    {
        Text text;

        void Awake()
        {
            text = transform.GetComponent<Text>();
        }

        public override void UpdateValue(string value)
        {
            text.text = value;
        }
    }
}
