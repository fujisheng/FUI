using FUI.Navigation;
using UnityEngine;

namespace FUI.Samples.Settings
{
    [AddComponentMenu("FUI/Samples/Settings Sample Bootstrap")]
    public sealed class SettingsSampleBootstrap : MonoBehaviour
    {
        Navigator navigator;
        ViewHandle handle;
        SettingsViewModel viewModel;

        void Start()
        {
            Routes.Initialize();
            navigator = new Navigator(new SettingsSampleViewProvider());
            navigator.Initialize();
            Open();
        }

        void OnDestroy()
        {
            if (viewModel != null)
            {
                viewModel.CloseRequested -= Close;
                viewModel = null;
            }
            navigator?.Shutdown();
            navigator = null;
        }

        internal void Close()
        {
            if (navigator != null && navigator.IsAlive(handle))
            {
                navigator.Close(handle);
            }
            if (viewModel != null)
            {
                viewModel.CloseRequested -= Close;
            }
        }

        void Open()
        {
            if (viewModel != null)
            {
                viewModel.CloseRequested -= Close;
            }
            viewModel = new SettingsViewModel();
            viewModel.CloseRequested += Close;
            handle = navigator.OpenWithViewModel(Routes.SettingsView, viewModel);
        }

        void OnGUI()
        {
            if (navigator != null && !navigator.IsAlive(handle))
            {
                if (GUI.Button(new Rect(20F, 20F, 180F, 40F), "Open Settings"))
                {
                    Open();
                }
            }
        }
    }

}
