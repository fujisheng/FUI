using FUI.Navigation;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using FUI.Resources;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Samples.Settings
{
    sealed class SettingsSampleViewProvider : IViewProvider
    {
        public IViewLease Create(RouteDescriptor route, IPreloadLease preload = null)
        {
            preload?.Dispose();
            EnsureEventSystem();
            var loader = new SettingsSampleAssetLoader();
            var viewObject = SettingsSampleViewFactory.Create(route.AssetKey);
            var view = View.Create(loader, viewObject, route.AssetKey);
            return new SettingsSampleViewLease(view, new SettingsSampleAssetScope());
        }

        public ValueTask<IViewLease> CreateAsync(
            RouteDescriptor route,
            IPreloadLease preload,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new ValueTask<IViewLease>(Create(route, preload));
        }

        public ValueTask<IPreloadLease> PreloadAsync(RouteDescriptor route, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new ValueTask<IPreloadLease>(new SettingsSamplePreloadLease(route));
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    sealed class SettingsSampleViewLease : IViewLease
    {
        bool disposed;

        public IView View { get; private set; }
        public IAssetScope Assets { get; }

        public SettingsSampleViewLease(IView view, IAssetScope assets)
        {
            View = view;
            Assets = assets;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            View?.Destroy();
            View = null;
            Assets.Dispose();
        }
    }

    sealed class SettingsSamplePreloadLease : IPreloadLease
    {
        public RouteDescriptor Route { get; }

        public SettingsSamplePreloadLease(RouteDescriptor route)
        {
            Route = route;
        }

        public void Dispose() { }
    }

    sealed class SettingsSampleAssetScope : IAssetScope
    {
        public IAssetLease<T> Load<T>(string key) => throw Unsupported();
        public ValueTask<IAssetLease<T>> LoadAsync<T>(string key, CancellationToken token) => throw Unsupported();
        public IAssetLease<T> Instantiate<T>(string key) => throw Unsupported();
        public ValueTask<IAssetLease<T>> InstantiateAsync<T>(string key, CancellationToken token) => throw Unsupported();
        public void Dispose() { }

        static NotSupportedException Unsupported()
        {
            return new NotSupportedException("The Settings Sample does not load dynamic assets.");
        }
    }

    sealed class SettingsSampleAssetLoader : IAssetLoader
    {
        public T Load<T>(string path) where T : Object => null;

        public ValueTask<T> LoadAsync<T>(string path, CancellationToken? cancellationToken = null)
            where T : Object => new ValueTask<T>((T)null);

        public GameObject CreateGameObject(string path) => null;

        public ValueTask<GameObject> CreateGameObjectAsync(
            string path,
            CancellationToken? cancellationToken = null) => new ValueTask<GameObject>((GameObject)null);

        public void DestroyGameObject(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }

        public void Release() { }
    }

    static class SettingsSampleViewFactory
    {
        static readonly Color PanelColor = new Color(0.10F, 0.12F, 0.17F, 0.98F);
        static readonly Color AccentColor = new Color(0.19F, 0.58F, 0.96F, 1F);
        static readonly Color TextColor = new Color(0.92F, 0.95F, 1F, 1F);

        public static GameObject Create(string viewName)
        {
            var root = new GameObject(
                viewName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920F, 1080F);

            var panel = CreateImage("Panel", root.transform, PanelColor);
            SetRect(panel.rectTransform, new Vector2(0.5F, 0.5F), new Vector2(640F, 520F), Vector2.zero);

            CreateTextElement("Title", panel.transform, "FUI Settings", 38, new Vector2(0F, 190F), new Vector2(540F, 60F));
            CreateTextElement("Status", panel.transform, string.Empty, 22, new Vector2(0F, -180F), new Vector2(540F, 50F));
            CreateSlider("Volume", panel.transform, new Vector2(0F, 70F));
            CreateToggle("Vibration", panel.transform, new Vector2(-180F, -25F));
            CreateButton("Close", panel.transform, "Close", new Vector2(0F, -105F));

            root.SetActive(false);
            return root;
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static void CreateTextElement(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Vector2 position,
            Vector2 size)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(LegacyTextElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            SetRect(gameObject.GetComponent<RectTransform>(), new Vector2(0.5F, 0.5F), size, position);
        }

        static void CreateSlider(string name, Transform parent, Vector2 position)
        {
            var track = CreateImage(name, parent, new Color(0.24F, 0.28F, 0.36F, 1F));
            track.gameObject.AddComponent<SliderElement>();
            SetRect(track.rectTransform, new Vector2(0.5F, 0.5F), new Vector2(440F, 24F), position);

            var fill = CreateImage("Fill", track.transform, AccentColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handle = CreateImage("Handle", track.transform, Color.white);
            SetRect(handle.rectTransform, new Vector2(0.5F, 0.5F), new Vector2(34F, 42F), Vector2.zero);

            var slider = track.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0F;
            slider.maxValue = 1F;
            slider.value = 0.5F;
        }

        static void CreateToggle(string name, Transform parent, Vector2 position)
        {
            var background = CreateImage(name, parent, new Color(0.24F, 0.28F, 0.36F, 1F));
            background.gameObject.AddComponent<ToggleElement>();
            SetRect(background.rectTransform, new Vector2(0.5F, 0.5F), new Vector2(42F, 42F), position);

            var check = CreateImage("Checkmark", background.transform, AccentColor);
            check.rectTransform.anchorMin = new Vector2(0.18F, 0.18F);
            check.rectTransform.anchorMax = new Vector2(0.82F, 0.82F);
            check.rectTransform.offsetMin = Vector2.zero;
            check.rectTransform.offsetMax = Vector2.zero;

            var toggle = background.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = true;

            CreateLabel("Label", background.transform, "Vibration", new Vector2(105F, 0F), new Vector2(160F, 42F));
        }

        static void CreateButton(string name, Transform parent, string label, Vector2 position)
        {
            var image = CreateImage(name, parent, AccentColor);
            image.gameObject.AddComponent<ButtonElement>();
            SetRect(image.rectTransform, new Vector2(0.5F, 0.5F), new Vector2(220F, 56F), position);
            image.GetComponent<Button>().targetGraphic = image;
            CreateLabel("Label", image.transform, label, Vector2.zero, new Vector2(220F, 56F));
        }

        static void CreateLabel(string name, Transform parent, string value, Vector2 position, Vector2 size)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(parent, false);
            var text = label.GetComponent<Text>();
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            SetRect(label.GetComponent<RectTransform>(), new Vector2(0.5F, 0.5F), size, position);
        }

        static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5F, 0.5F);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
