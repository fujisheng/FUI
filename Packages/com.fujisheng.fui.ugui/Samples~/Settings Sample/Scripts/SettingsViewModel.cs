using FUI.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Rendering;
using System;

namespace FUI.Samples.Settings
{
    [ViewContract("SettingsView")]
    [RoutePolicy(Layer.Popup, CoverageMode = CoverageMode.KeepVisible)]
    public partial class SettingsViewModel : ViewModel
    {
        [ObservableProperty]
        [Bind("Title", nameof(ITextElement.Text))]
        string title = "FUI Settings";

        [ObservableProperty]
        [Bind("Volume", nameof(ISliderElement.Value), bindingMode: BindingMode.TwoWay)]
        float volume = 0.5F;

        [ObservableProperty]
        [Bind("Vibration", nameof(IToggleElement.IsOn), bindingMode: BindingMode.TwoWay)]
        bool vibrationEnabled = true;

        [ObservableProperty]
        [Bind("Status", nameof(ITextElement.Text))]
        string status = "Adjust a control to verify two-way binding.";

        /// <summary>通知当前视图宿主关闭页面，不依赖静态 Unity Bootstrap。</summary>
        public event Action CloseRequested;

        [Command("Volume", nameof(ISliderElement.ValueChanged))]
        public void OnVolumeChanged(float value)
        {
            Status = $"Volume: {value:0.00}";
        }

        [Command("Vibration", nameof(IToggleElement.ValueChanged))]
        public void OnVibrationChanged(bool enabled)
        {
            Status = enabled ? "Vibration enabled" : "Vibration disabled";
        }

        [Command("Close", nameof(IButtonElement.Click))]
        public void Close()
        {
            CloseRequested?.Invoke();
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
