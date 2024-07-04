using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(Text))]
    public class TextElement : UGUIVisualElement<string>
    {
        Text text;

        protected override void Initialize()
        {
            text = transform.GetComponent<Text>();
        }

        public override void UpdateValue(string value)
        {
            text.text = value;
        }

        public override void UpdateValue(object value)
        {
            text.text = value?.ToString();
        }
    }
}
