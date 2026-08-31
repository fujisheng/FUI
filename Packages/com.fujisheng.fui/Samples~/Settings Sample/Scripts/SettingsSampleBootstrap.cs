using FUI.Navigation;
using UnityEngine;

namespace FUI.Samples.Settings
{
    [AddComponentMenu("FUI/Samples/Settings Sample Bootstrap")]
    public sealed class SettingsSampleBootstrap : MonoBehaviour
    {
        Navigator navigator;
        ViewHandle handle;

        void Start()
        {
            Routes.Initialize();
            navigator = new Navigator(new SettingsSampleViewProvider());
            navigator.Initialize();
            SettingsSampleRuntime.Attach(this);
            Open();
        }

        void OnDestroy()
        {
            SettingsSampleRuntime.Detach(this);
            navigator?.Shutdown();
            navigator = null;
        }

        internal void Close()
        {
            if (navigator != null && navigator.IsAlive(handle))
            {
                navigator.Close(handle);
            }
        }

        void Open()
        {
            handle = navigator.Open(Routes.SettingsView);
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

    static class SettingsSampleRuntime
    {
        static SettingsSampleBootstrap owner;

        public static void Attach(SettingsSampleBootstrap bootstrap)
        {
            owner = bootstrap;
        }

        public static void Detach(SettingsSampleBootstrap bootstrap)
        {
            if (owner == bootstrap)
            {
                owner = null;
            }
        }

        public static void Close()
        {
            owner?.Close();
        }
    }
}
