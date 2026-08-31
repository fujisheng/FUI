using FUI.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Rendering.UGUI.Elements;

namespace FUI.Samples.Settings
{
    [ViewContract("SettingsView")]
    [RoutePolicy(Layer.Popup, CoverageMode = CoverageMode.KeepVisible)]
    public partial class SettingsViewModel : ViewModel
    {
        [ObservableProperty]
        [Bind("Title", nameof(TextElement.Content))]
        string title = "FUI Settings";

        [ObservableProperty]
        [Bind("Volume", nameof(SliderElement.Value), bindingMode: BindingMode.TwoWay)]
        float volume = 0.5F;

        [ObservableProperty]
        [Bind("Vibration", nameof(ToggleElement.IsOn), bindingMode: BindingMode.TwoWay)]
        bool vibrationEnabled = true;

        [ObservableProperty]
        [Bind("Status", nameof(TextElement.Content))]
        string status = "Adjust a control to verify two-way binding.";

        [Command("Volume", nameof(SliderElement.OnValueChanged))]
        public void OnVolumeChanged(float value)
        {
            Status = $"Volume: {value:0.00}";
        }

        [Command("Vibration", nameof(ToggleElement.OnValueChanged))]
        public void OnVibrationChanged(bool enabled)
        {
            Status = enabled ? "Vibration enabled" : "Vibration disabled";
        }

        [Command("Close", nameof(ButtonElement.OnClick))]
        public void Close()
        {
            SettingsSampleRuntime.Close();
        }
    }

    public sealed class SettingsPresenter : Presenter<SettingsViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.Status = "FUI binding is active.";
        }
    }
}
