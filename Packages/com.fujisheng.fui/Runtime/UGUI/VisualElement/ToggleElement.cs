using UnityEngine.UI;

namespace FUI.UGUI.VisualElement
{
    public class ToggleElement : UGUIVisualElement<bool>, IObservableVisualElement<bool>
    {
        Toggle toggle;

        protected override void Initialize()
        {
            toggle = transform.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener((value) =>
            {
                OnValueChanged?.Invoke(value);
            });
        }

        public override void UpdateValue(bool value)
        {
            toggle.isOn = value;
        }

        public event VisualElementValueChangedHandler<bool> OnValueChanged;
    }
}