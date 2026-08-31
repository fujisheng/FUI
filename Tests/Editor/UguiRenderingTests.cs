using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FUI.Diagnostics;
using FUI.Resources;
using FUI.Rendering.UGUI.Elements;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Tests
{
    public sealed class UguiRenderingTests
    {
        sealed class LogSinkProbe : ILogSink
        {
            public readonly List<string> Errors = new List<string>();

            public void Log(object message)
            {
            }

            public void LogWarning(object message)
            {
            }

            public void LogError(object message)
            {
                Errors.Add(message?.ToString());
            }

            public void LogException(Exception exception)
            {
            }
        }

        sealed class LifetimeProbeElement : RectTransformElement
        {
            public readonly List<string> Events = new List<string>();
            public bool ThrowDuringInitialization { get; set; }

            protected override void OnInitialize()
            {
                base.OnInitialize();
                TrackCleanup(() => Events.Add("first-cleanup"));
                TrackCleanup(() => Events.Add("second-cleanup"));
                if (ThrowDuringInitialization)
                {
                    throw new InvalidOperationException("Expected initialization failure.");
                }
            }

            protected override void OnRelease()
            {
                // 故意不调用 base，验证基类最终清理不依赖派生类实现。
                Events.Add("release-hook");
            }
        }

        sealed class CustomTextGraphic : MaskableGraphic
        {
            public string Value;
            public float Size = 14f;
            public TextAnchor Anchor = TextAnchor.UpperLeft;
            public bool SupportsRichText = true;

            protected override void OnPopulateMesh(VertexHelper vertexHelper)
            {
                vertexHelper.Clear();
            }
        }

        sealed class CustomTextElement : TextElement<CustomTextGraphic>
        {
            protected override string TextValue
            {
                get => Component.Value;
                set => Component.Value = value;
            }

            protected override float FontSizeValue
            {
                get => Component.Size;
                set => Component.Size = value;
            }

            protected override TextAnchor AlignmentValue
            {
                get => Component.Anchor;
                set => Component.Anchor = value;
            }

            protected override bool RichTextValue
            {
                get => Component.SupportsRichText;
                set => Component.SupportsRichText = value;
            }
        }

        sealed class AssetLease<T> : IAssetLease<T>
        {
            public T Value { get; }
            public int DisposeCount { get; private set; }

            public AssetLease(T value)
            {
                Value = value;
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        sealed class AssetLoaderProbe : ILeaseAssetLoader
        {
            readonly Dictionary<string, UnityEngine.Object> assets = new Dictionary<string, UnityEngine.Object>();

            public readonly List<object> CreatedLeases = new List<object>();
            public string ThrowingSource { get; set; }

            public void Register(string source, UnityEngine.Object value)
            {
                assets[source] = value;
            }

            public T Load<T>(string path) where T : UnityEngine.Object
            {
                return assets.TryGetValue(path, out var value) ? value as T : null;
            }

            public ValueTask<T> LoadAsync<T>(string path, CancellationToken? cancellationToken = null)
                where T : UnityEngine.Object
            {
                return new ValueTask<T>(Load<T>(path));
            }

            public GameObject CreateGameObject(string path) => Load<GameObject>(path);

            public ValueTask<GameObject> CreateGameObjectAsync(
                string path,
                CancellationToken? cancellationToken = null)
            {
                return new ValueTask<GameObject>(CreateGameObject(path));
            }

            public void DestroyGameObject(GameObject gameObject)
            {
            }

            public void Release()
            {
            }

            public IAssetLease<T> LoadLease<T>(string path) where T : UnityEngine.Object
            {
                if (path == ThrowingSource)
                {
                    throw new InvalidOperationException("Expected load failure.");
                }
                var lease = new AssetLease<T>(Load<T>(path));
                CreatedLeases.Add(lease);
                return lease;
            }

            public IAssetLease<GameObject> InstantiateLease(string path)
            {
                var lease = new AssetLease<GameObject>(CreateGameObject(path));
                CreatedLeases.Add(lease);
                return lease;
            }
        }

        GameObject root;

        [TearDown]
        public void TearDown()
        {
            Log.Instance.SetLogger(null);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ViewInitializationMapsStateToCanvasAndCanvasGroup()
        {
            root = new GameObject("StatusView(Clone)", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var view = root.AddComponent<View>();
            var canvas = root.GetComponent<Canvas>();
            var canvasGroup = root.GetComponent<CanvasGroup>();
            var runtimeView = (IView)view;

            runtimeView.Initialize();
            runtimeView.Layer = 300;
            runtimeView.Order = 7;
            runtimeView.Interactable = false;
            runtimeView.Visible = false;

            Assert.That(runtimeView.Name, Is.EqualTo("StatusView"));
            Assert.That(canvas.sortingOrder, Is.EqualTo(307));
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
            Assert.That(root.activeSelf, Is.False);

            runtimeView.Destroy();
            Assert.That(view.Visible.Value, Is.False);
            Assert.That(view.Layer.Value, Is.Zero);
        }

        [Test]
        public void ViewDiscoversElementsByExactAndAssignableType()
        {
            root = new GameObject("InventoryView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Content", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var element = child.AddComponent<RectTransformElement>();
            var runtimeView = (IView)view;

            runtimeView.Initialize();

            Assert.That(runtimeView.GetElement<RectTransformElement>("Content"), Is.SameAs(element));
            Assert.That(runtimeView.GetElement("Content", typeof(Element)), Is.SameAs(element));
            Assert.That(runtimeView.GetElement<View>(Element.RootName), Is.SameAs(view));
            Assert.That(runtimeView.GetElement<Element>("Missing"), Is.Null);
            Assert.Throws<System.Exception>(() => runtimeView.GetElement(string.Empty, typeof(Element)));
        }

        [Test]
        public void RootAliasDoesNotDuplicateElementOwnership()
        {
            root = new GameObject("AliasView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Content", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            child.AddComponent<RectTransformElement>();
            var runtimeView = (IView)view;

            runtimeView.Initialize();

            Assert.That(view.Elements.Count, Is.EqualTo(2));
            Assert.That(runtimeView.GetElement<View>(Element.RootName), Is.SameAs(view));
        }

        [Test]
        public void ViewInitializationIsIdempotent()
        {
            root = new GameObject("IdempotentView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Content", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            child.AddComponent<RectTransformElement>();
            var runtimeView = (IView)view;

            runtimeView.Initialize();
            runtimeView.Initialize();

            Assert.That(view.Elements.Count, Is.EqualTo(2));
            Assert.DoesNotThrow(runtimeView.Destroy);
            Assert.DoesNotThrow(runtimeView.Destroy);
        }

        [Test]
        public void RegisteringSameElementInstanceAgainIsSilentAndIdempotent()
        {
            root = new GameObject("SameInstanceView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Content", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var element = child.AddComponent<RectTransformElement>();
            var logSink = new LogSinkProbe();
            Log.Instance.SetLogger(logSink);

            ((IView)view).Initialize();
            typeof(View)
                .GetMethod("AddElement", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(view, new object[] { element });

            Assert.That(view.Elements.Count(item => ReferenceEquals(item, element)), Is.EqualTo(1));
            Assert.That(logSink.Errors, Is.Empty);
        }

        [Test]
        public void ElementReleaseRunsLifetimeCleanupInReverseOrderWithoutBaseCall()
        {
            root = new GameObject("LifetimeView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Probe", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var probe = child.AddComponent<LifetimeProbeElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            runtimeView.Destroy();
            runtimeView.Destroy();

            Assert.That(probe.Events, Is.EqualTo(new[]
            {
                "release-hook",
                "second-cleanup",
                "first-cleanup",
            }));
        }

        [Test]
        public void FailedElementInitializationRollsBackTrackedResources()
        {
            root = new GameObject("RollbackView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Probe", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var probe = child.AddComponent<LifetimeProbeElement>();
            probe.ThrowDuringInitialization = true;

            Assert.Throws<InvalidOperationException>(() => ((IView)view).Initialize());
            Assert.That(probe.Events, Is.EqualTo(new[] { "second-cleanup", "first-cleanup" }));
            Assert.That(view.Elements, Is.Empty);
        }

        [Test]
        public void DuplicateElementUsesFirstAndReleasesIgnoredElement()
        {
            root = new GameObject("DuplicateView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var firstObject = new GameObject("Entry", typeof(RectTransform));
            firstObject.transform.SetParent(root.transform, false);
            var first = firstObject.AddComponent<LifetimeProbeElement>();
            var secondObject = new GameObject("Entry", typeof(RectTransform));
            secondObject.transform.SetParent(root.transform, false);
            var second = secondObject.AddComponent<LifetimeProbeElement>();
            var logSink = new LogSinkProbe();
            Log.Instance.SetLogger(logSink);

            Assert.DoesNotThrow(() => ((IView)view).Initialize());

            Assert.That(((IView)view).GetElement<LifetimeProbeElement>("Entry"), Is.SameAs(first));
            Assert.That(first.Events, Is.Empty);
            Assert.That(second.Events, Does.Contain("first-cleanup"));
            Assert.That(view.Elements, Does.Contain(first));
            Assert.That(view.Elements.Contains(second), Is.False);
            Assert.That(logSink.Errors, Has.Count.EqualTo(1));
            Assert.That(logSink.Errors[0], Does.Contain("The first registered element will be used"));

            ((IView)view).Destroy();
            Assert.That(first.Events, Does.Contain("first-cleanup"));
        }

        [Test]
        public void MutuallyExclusiveElementFamiliesDisallowMultipleComponents()
        {
            var exclusiveTypes = new[]
            {
                typeof(View),
                typeof(TextElement),
                typeof(LegacyTextElement),
                typeof(TMPTextElement),
                typeof(InputFieldElement),
                typeof(LegacyInputFieldElement),
                typeof(TMPInputFieldElement),
                typeof(DropdownElement),
                typeof(LegacyDropdownElement),
                typeof(TMPDropdownElement),
                typeof(ListElement),
                typeof(RecyclingListElement),
                typeof(SlotListElement),
                typeof(ScrollListElement),
                typeof(AnchoredListElement),
                typeof(NestedViewElement),
                typeof(DynamicViewElement),
                typeof(ImageElement),
                typeof(RawImageElement),
                typeof(ButtonElement),
                typeof(ToggleElement),
                typeof(SliderElement),
                typeof(ScrollbarElement),
                typeof(ScrollViewElement),
                typeof(CanvasGroupElement),
                typeof(InteractionElement),
                typeof(FUI.Rendering.UGUI.Elements.LayoutElement),
                typeof(MaskElement),
                typeof(RectMaskElement),
                typeof(ScreenPositionElement),
                typeof(ToggleGroupElement),
                typeof(AspectRatioElement),
                typeof(ShadowElement),
                typeof(OutlineElement),
                typeof(ViewPrototypeElement),
            };

            foreach (var type in exclusiveTypes)
            {
                Assert.That(
                    type.GetCustomAttribute<DisallowMultipleComponent>(true),
                    Is.Not.Null,
                    $"{type.FullName} must disallow multiple components.");
            }
        }

        [Test]
        public void RootContainerStopsViewTraversalAtItsOwnProjectionBoundary()
        {
            root = new GameObject("GoodsItem", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var container = root.AddComponent<SlotListElement>();
            typeof(SlotListElement)
                .GetField("content", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(container, root.transform);

            var firstSlot = new GameObject("Img_Icon", typeof(RectTransform));
            firstSlot.transform.SetParent(root.transform, false);
            firstSlot.AddComponent<RectTransformElement>();
            var secondSlot = new GameObject("Img_Icon", typeof(RectTransform));
            secondSlot.transform.SetParent(root.transform, false);
            secondSlot.AddComponent<RectTransformElement>();

            Assert.DoesNotThrow(() => ((IView)view).Initialize());
            Assert.That(view.Elements, Is.EquivalentTo(new IElement[] { view, container }));
            Assert.That(((IView)view).GetElement<RectTransformElement>("Img_Icon"), Is.Null);
        }

        [Test]
        public void SliderBindingDoesNotInvokeCommandButUserEventInvokesOnce()
        {
            root = new GameObject("ControlView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            child.transform.SetParent(root.transform, false);
            var slider = child.GetComponent<Slider>();
            var element = child.AddComponent<SliderElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();
            var invocationCount = 0;
            element.OnValueChanged.AddListener(value => invocationCount++);

            element.Value.Value = 0.25f;
            element.Interactable.Value = false;
            var colors = slider.colors;
            colors.normalColor = Color.red;
            colors.highlightedColor = Color.green;
            colors.pressedColor = Color.blue;
            colors.selectedColor = Color.yellow;
            colors.disabledColor = Color.gray;
            colors.colorMultiplier = 1.5f;
            colors.fadeDuration = 0.25f;
            element.Colors.Value = colors;

            Assert.That(slider.value, Is.EqualTo(0.25f));
            Assert.That(slider.interactable, Is.False);
            Assert.That(element.NormalColor.Value, Is.EqualTo(Color.red));
            Assert.That(element.HighlightedColor.Value, Is.EqualTo(Color.green));
            Assert.That(element.PressedColor.Value, Is.EqualTo(Color.blue));
            Assert.That(element.SelectedColor.Value, Is.EqualTo(Color.yellow));
            Assert.That(element.DisabledColor.Value, Is.EqualTo(Color.gray));
            Assert.That(element.ColorMultiplier.Value, Is.EqualTo(1.5f));
            Assert.That(element.FadeDuration.Value, Is.EqualTo(0.25f));
            Assert.That(invocationCount, Is.Zero);

            element.NormalColor.Value = Color.cyan;
            Assert.That(slider.colors.normalColor, Is.EqualTo(Color.cyan));
            Assert.That(element.Colors.Value.normalColor, Is.EqualTo(Color.cyan));

            slider.onValueChanged.Invoke(0.75f);

            Assert.That(element.Value.Value, Is.EqualTo(0.75f));
            Assert.That(invocationCount, Is.EqualTo(1));

            runtimeView.Destroy();
            slider.onValueChanged.Invoke(0.5f);
            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void FormattedTextIsNullSafeImmutableAndCultureAware()
        {
            var defaultValue = default(FormattedText);
            Assert.That(defaultValue.ToString(), Is.Empty);
            Assert.That(defaultValue.Equals(default(FormattedText)), Is.True);
            Assert.DoesNotThrow(() => defaultValue.GetHashCode());

            var arguments = new object[] { 12.5m, null };
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            var value = new FormattedText("{0:N1}-{1}", culture, arguments);
            var equalValue = new FormattedText("{0:N1}-{1}", culture, 12.5m, null);
            arguments[0] = 99m;

            Assert.That(value.ToString(), Is.EqualTo("12,5-"));
            Assert.That(value, Is.EqualTo(equalValue));
            Assert.That(value.GetHashCode(), Is.EqualTo(equalValue.GetHashCode()));
        }

        [Test]
        public void ImageSourceSwitchIsTransactionalAndReleasesEachLeaseOnce()
        {
            var texture = new Texture2D(2, 2);
            var firstSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            var secondSprite = Sprite.Create(texture, new Rect(1, 1, 1, 1), Vector2.zero);
            try
            {
                var loader = new AssetLoaderProbe();
                loader.Register("first", firstSprite);
                loader.Register("second", secondSprite);
                root = new GameObject("ImageView", typeof(RectTransform));
                var view = View.Create(loader, root);
                var child = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                child.transform.SetParent(root.transform, false);
                var image = child.GetComponent<Image>();
                var element = child.AddComponent<ImageElement>();
                var runtimeView = (IView)view;
                runtimeView.Initialize();

                element.Color.Value = Color.magenta;
                element.RaycastTarget.Value = false;
                element.Maskable.Value = false;
                Assert.That(image.color, Is.EqualTo(Color.magenta));
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(image.maskable, Is.False);

                element.SpriteSource.Value = "first";
                var firstLease = (AssetLease<Sprite>)loader.CreatedLeases[0];
                Assert.That(image.sprite, Is.SameAs(firstSprite));
                Assert.That(firstLease.DisposeCount, Is.Zero);

                loader.ThrowingSource = "throws";
                element.SpriteSource.Value = "throws";
                Assert.That(image.sprite, Is.SameAs(firstSprite));
                Assert.That(element.SpriteSource.Value, Is.EqualTo("first"));
                Assert.That(firstLease.DisposeCount, Is.Zero);

                element.SpriteSource.Value = "missing";
                var failedLease = (AssetLease<Sprite>)loader.CreatedLeases[1];
                Assert.That(image.sprite, Is.SameAs(firstSprite));
                Assert.That(element.SpriteSource.Value, Is.EqualTo("first"));
                Assert.That(firstLease.DisposeCount, Is.Zero);
                Assert.That(failedLease.DisposeCount, Is.EqualTo(1));

                element.SpriteSource.Value = "second";
                var secondLease = (AssetLease<Sprite>)loader.CreatedLeases[2];
                Assert.That(image.sprite, Is.SameAs(secondSprite));
                Assert.That(firstLease.DisposeCount, Is.EqualTo(1));

                element.SpriteSource.Value = null;
                Assert.That(image.sprite, Is.Null);
                Assert.That(secondLease.DisposeCount, Is.EqualTo(1));

                element.SpriteSource.Value = "second";
                var replacementLease = (AssetLease<Sprite>)loader.CreatedLeases[3];
                element.Sprite.Value = firstSprite;
                Assert.That(element.SpriteSource.Value, Is.Null);
                Assert.That(replacementLease.DisposeCount, Is.EqualTo(1));

                runtimeView.Destroy();
                Assert.That(firstLease.DisposeCount, Is.EqualTo(1));
                Assert.That(secondLease.DisposeCount, Is.EqualTo(1));
                Assert.That(replacementLease.DisposeCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(firstSprite);
                Object.DestroyImmediate(secondSprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void DynamicViewSourceWithoutViewModelKeepsVisualInstanceActive()
        {
            var loader = new AssetLoaderProbe();
            var visualInstance = new GameObject("HeroVisual", typeof(RectTransform));
            loader.Register("hero", visualInstance);
            root = new GameObject("DynamicViewHost", typeof(RectTransform));
            var view = View.Create(loader, root);
            var host = new GameObject("HeroRoot", typeof(RectTransform));
            host.transform.SetParent(root.transform, false);
            var element = host.AddComponent<DynamicViewElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            element.Source.Value = "hero";

            Assert.That(visualInstance.transform.parent, Is.SameAs(host.transform));
            Assert.That(visualInstance.activeSelf, Is.True);
            Assert.That(visualInstance.activeInHierarchy, Is.True);

            element.Source.Value = null;
            var lease = (AssetLease<GameObject>)loader.CreatedLeases[0];
            Assert.That(lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void CommonElementsSynchronizeComponentState()
        {
            root = new GameObject("CommonElementsView", typeof(RectTransform));
            var view = root.AddComponent<View>();

            var layoutObject = new GameObject(
                "Layout",
                typeof(RectTransform),
                typeof(UnityEngine.UI.LayoutElement));
            layoutObject.transform.SetParent(root.transform, false);
            var layoutComponent = layoutObject.GetComponent<UnityEngine.UI.LayoutElement>();
            var layoutElement = layoutObject.AddComponent<FUI.Rendering.UGUI.Elements.LayoutElement>();

            var maskObject = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskObject.transform.SetParent(root.transform, false);
            var maskComponent = maskObject.GetComponent<Mask>();
            var maskElement = maskObject.AddComponent<MaskElement>();

            var rectMaskObject = new GameObject("RectMask", typeof(RectTransform), typeof(RectMask2D));
            rectMaskObject.transform.SetParent(root.transform, false);
            var rectMaskComponent = rectMaskObject.GetComponent<RectMask2D>();
            var rectMaskElement = rectMaskObject.AddComponent<RectMaskElement>();

            var toggleGroupObject = new GameObject("ToggleGroup", typeof(RectTransform), typeof(ToggleGroup));
            toggleGroupObject.transform.SetParent(root.transform, false);
            var toggleGroup = toggleGroupObject.GetComponent<ToggleGroup>();
            var toggleGroupElement = toggleGroupObject.AddComponent<ToggleGroupElement>();

            var aspectObject = new GameObject("Aspect", typeof(RectTransform), typeof(AspectRatioFitter));
            aspectObject.transform.SetParent(root.transform, false);
            var aspect = aspectObject.GetComponent<AspectRatioFitter>();
            var aspectElement = aspectObject.AddComponent<AspectRatioElement>();

            var shadowObject = new GameObject("Shadow", typeof(RectTransform), typeof(Image), typeof(Shadow));
            shadowObject.transform.SetParent(root.transform, false);
            var shadow = shadowObject.GetComponent<Shadow>();
            var shadowElement = shadowObject.AddComponent<ShadowElement>();

            var outlineObject = new GameObject("Outline", typeof(RectTransform), typeof(Image), typeof(Outline));
            outlineObject.transform.SetParent(root.transform, false);
            var outline = outlineObject.GetComponent<Outline>();
            var outlineElement = outlineObject.AddComponent<OutlineElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            layoutElement.IgnoreLayout.Value = true;
            layoutElement.MinWidth.Value = 10f;
            layoutElement.MinHeight.Value = 20f;
            layoutElement.PreferredWidth.Value = 30f;
            layoutElement.PreferredHeight.Value = 40f;
            layoutElement.FlexibleWidth.Value = 1f;
            layoutElement.FlexibleHeight.Value = 2f;
            layoutElement.LayoutPriority.Value = 3;
            maskElement.ShowMaskGraphic.Value = false;
            rectMaskElement.Padding.Value = new Vector4(1f, 2f, 3f, 4f);
            rectMaskElement.Softness.Value = new Vector2Int(5, 6);
            toggleGroupElement.AllowSwitchOff.Value = true;
            aspectElement.Mode.Value = AspectRatioFitter.AspectMode.FitInParent;
            aspectElement.AspectRatio.Value = 1.5f;
            shadowElement.EffectColor.Value = Color.red;
            shadowElement.EffectDistance.Value = new Vector2(2f, -3f);
            shadowElement.UseGraphicAlpha.Value = false;
            outlineElement.EffectColor.Value = Color.blue;

            Assert.That(layoutComponent.ignoreLayout, Is.True);
            Assert.That(layoutComponent.minWidth, Is.EqualTo(10f));
            Assert.That(layoutComponent.minHeight, Is.EqualTo(20f));
            Assert.That(layoutComponent.preferredWidth, Is.EqualTo(30f));
            Assert.That(layoutComponent.preferredHeight, Is.EqualTo(40f));
            Assert.That(layoutComponent.flexibleWidth, Is.EqualTo(1f));
            Assert.That(layoutComponent.flexibleHeight, Is.EqualTo(2f));
            Assert.That(layoutComponent.layoutPriority, Is.EqualTo(3));
            Assert.That(maskComponent.showMaskGraphic, Is.False);
            Assert.That(rectMaskComponent.padding, Is.EqualTo(new Vector4(1f, 2f, 3f, 4f)));
            Assert.That(rectMaskComponent.softness, Is.EqualTo(new Vector2Int(5, 6)));
            Assert.That(toggleGroup.allowSwitchOff, Is.True);
            Assert.That(aspect.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(aspect.aspectRatio, Is.EqualTo(1.5f));
            Assert.That(shadow.effectColor, Is.EqualTo(Color.red));
            Assert.That(shadow.effectDistance, Is.EqualTo(new Vector2(2f, -3f)));
            Assert.That(shadow.useGraphicAlpha, Is.False);
            Assert.That(outline.effectColor, Is.EqualTo(Color.blue));
        }

        [Test]
        public void ScrollViewSynchronizesStateAndStopsCallbacksAfterRelease()
        {
            root = new GameObject("ScrollView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var scrollObject = new GameObject("Scroller", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(root.transform, false);
            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(scrollObject.transform, false);
            scrollRect.content = content;
            var element = scrollObject.AddComponent<ScrollViewElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();
            var callbackCount = 0;
            element.OnValueChanged.AddListener(value => callbackCount++);

            element.ContentPosition.Value = new Vector2(12f, 34f);
            element.Velocity.Value = new Vector2(5f, 6f);
            element.Horizontal.Value = false;
            element.Vertical.Value = true;
            scrollRect.onValueChanged.Invoke(new Vector2(0.2f, 0.8f));

            Assert.That(content.anchoredPosition, Is.EqualTo(new Vector2(12f, 34f)));
            Assert.That(scrollRect.velocity, Is.EqualTo(new Vector2(5f, 6f)));
            Assert.That(scrollRect.horizontal, Is.False);
            Assert.That(scrollRect.vertical, Is.True);
            Assert.That(callbackCount, Is.EqualTo(1));

            runtimeView.Destroy();
            scrollRect.onValueChanged.Invoke(Vector2.zero);
            Assert.That(callbackCount, Is.EqualTo(1));
        }

        [Test]
        public void InteractionElementPreservesEventTriggerEntriesAndUsesTypedEvents()
        {
            root = new GameObject("InteractionView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Interaction", typeof(RectTransform), typeof(EventTrigger));
            child.transform.SetParent(root.transform, false);
            var eventTrigger = child.GetComponent<EventTrigger>();
            eventTrigger.triggers.Add(new EventTrigger.Entry { eventID = EventTriggerType.PointerClick });
            var element = child.AddComponent<InteractionElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();
            PointerEventData received = null;
            element.OnPointerClick.AddListener(value => received = value);
            var eventData = new PointerEventData(null);

            ((IPointerClickHandler)element).OnPointerClick(eventData);

            Assert.That(received, Is.SameAs(eventData));
            Assert.That(eventTrigger.triggers.Count, Is.EqualTo(1));
            Assert.That(eventTrigger.triggers[0].eventID, Is.EqualTo(EventTriggerType.PointerClick));
        }

        [Test]
        public void LegacyAndTmpTextElementsExposeConsistentBindingBehavior()
        {
            root = new GameObject("TextView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var legacyObject = new GameObject("Legacy", typeof(RectTransform), typeof(Text));
            legacyObject.transform.SetParent(root.transform, false);
            var legacyElement = legacyObject.AddComponent<LegacyTextElement>();
            var tmpObject = new GameObject("Tmp", typeof(RectTransform), typeof(TextMeshProUGUI));
            tmpObject.transform.SetParent(root.transform, false);
            var tmpElement = tmpObject.AddComponent<TMPTextElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            Assert.That(runtimeView.GetElement<TextElement>("Legacy"), Is.SameAs(legacyElement));
            Assert.That(runtimeView.GetElement<TextElement>("Tmp"), Is.SameAs(tmpElement));

            legacyElement.Text.Value = "plain";
            tmpElement.Text.Value = "plain";
            legacyElement.Content.Value = 42;
            tmpElement.Content.Value = 42;
            legacyElement.FormattedText.Value = new FormattedText("Value {0}", 7);
            tmpElement.FormattedText.Value = new FormattedText("Value {0}", 7);
            legacyElement.Color.Value = Color.green;
            tmpElement.Color.Value = Color.green;
            legacyElement.Alignment.Value = TextAnchor.MiddleCenter;
            tmpElement.Alignment.Value = TextAnchor.MiddleCenter;
            legacyElement.FontSize.Value = 13.6f;
            tmpElement.FontSize.Value = 13.6f;
            legacyElement.RichText.Value = false;
            tmpElement.RichText.Value = false;
            legacyElement.BestFit.Value = true;
            tmpElement.AutoSize.Value = true;

            var legacyText = legacyObject.GetComponent<Text>();
            var tmpText = tmpObject.GetComponent<TextMeshProUGUI>();
            Assert.That(legacyText.text, Is.EqualTo("Value 7"));
            Assert.That(tmpText.text, Is.EqualTo("Value 7"));
            Assert.That(legacyText.color, Is.EqualTo(Color.green));
            Assert.That(tmpText.color, Is.EqualTo(Color.green));
            Assert.That(legacyText.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
            Assert.That(tmpElement.Alignment.Value, Is.EqualTo(TextAnchor.MiddleCenter));
            Assert.That(legacyText.fontSize, Is.EqualTo(14));
            Assert.That(tmpText.fontSize, Is.EqualTo(13.6f));
            Assert.That(legacyText.supportRichText, Is.False);
            Assert.That(tmpText.richText, Is.False);
            Assert.That(legacyText.resizeTextForBestFit, Is.True);
            Assert.That(tmpText.enableAutoSizing, Is.True);

            var alignments = new[]
            {
                TextAnchor.UpperLeft,
                TextAnchor.UpperCenter,
                TextAnchor.UpperRight,
                TextAnchor.MiddleLeft,
                TextAnchor.MiddleCenter,
                TextAnchor.MiddleRight,
                TextAnchor.LowerLeft,
                TextAnchor.LowerCenter,
                TextAnchor.LowerRight,
            };
            foreach (var alignment in alignments)
            {
                tmpElement.Alignment.Value = alignment;
                Assert.That(tmpElement.Alignment.Value, Is.EqualTo(alignment));
            }
        }

        [Test]
        public void CustomTextElementUsesPrefabComponentAndFailsWhenItIsMissing()
        {
            root = new GameObject("CustomTextView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Custom", typeof(RectTransform), typeof(CustomTextGraphic));
            child.transform.SetParent(root.transform, false);
            var component = child.GetComponent<CustomTextGraphic>();
            var element = child.AddComponent<CustomTextElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            element.Content.Value = 123;
            element.FontSize.Value = 19f;
            element.Alignment.Value = TextAnchor.MiddleCenter;
            element.RichText.Value = false;

            Assert.That(component.Value, Is.EqualTo("123"));
            Assert.That(component.Size, Is.EqualTo(19f));
            Assert.That(component.Anchor, Is.EqualTo(TextAnchor.MiddleCenter));
            Assert.That(component.SupportsRichText, Is.False);
            Assert.That(runtimeView.GetElement<TextElement>("Custom"), Is.SameAs(element));

            runtimeView.Destroy();
            Object.DestroyImmediate(root);
            root = new GameObject("MissingCustomTextView", typeof(RectTransform));
            view = root.AddComponent<View>();
            child = new GameObject("Missing", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            child.AddComponent<CustomTextElement>();

            Assert.Throws<MissingComponentException>(() => ((IView)view).Initialize());
        }

        [Test]
        public void LegacyAndTmpInputFieldsShareBehaviorWithoutCommandFeedback()
        {
            root = new GameObject("InputView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var legacyObject = new GameObject("LegacyInput", typeof(RectTransform), typeof(InputField));
            legacyObject.transform.SetParent(root.transform, false);
            var legacyComponent = legacyObject.GetComponent<InputField>();
            var legacyElement = legacyObject.AddComponent<LegacyInputFieldElement>();
            var tmpObject = new GameObject("TmpInput", typeof(RectTransform), typeof(TMP_InputField));
            tmpObject.transform.SetParent(root.transform, false);
            var tmpComponent = tmpObject.GetComponent<TMP_InputField>();
            var tmpElement = tmpObject.AddComponent<TMPInputFieldElement>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();
            var legacyCalls = 0;
            var tmpCalls = 0;
            legacyElement.OnValueChanged.AddListener(value => legacyCalls++);
            tmpElement.OnValueChanged.AddListener(value => tmpCalls++);

            legacyElement.Text.Value = "legacy";
            tmpElement.Text.Value = "tmp";
            Assert.That(legacyCalls, Is.Zero, "Text binding must not invoke the Legacy command.");
            Assert.That(tmpCalls, Is.Zero, "Text binding must not invoke the TMP command.");
            legacyElement.ContentType.Value = InputContentType.EmailAddress;
            tmpElement.ContentType.Value = InputContentType.EmailAddress;
            Assert.That(legacyCalls, Is.Zero, "ContentType binding must not invoke the Legacy command.");
            Assert.That(tmpCalls, Is.Zero, "ContentType binding must not invoke the TMP command.");
            legacyElement.LineType.Value = InputLineType.MultiLineNewline;
            tmpElement.LineType.Value = InputLineType.MultiLineNewline;
            Assert.That(legacyCalls, Is.Zero, "LineType binding must not invoke the Legacy command.");
            Assert.That(tmpCalls, Is.Zero, "LineType binding must not invoke the TMP command.");
            legacyElement.CaretWidth.Value = 3;
            tmpElement.CaretWidth.Value = 4;
            tmpElement.RichText.Value = false;
            tmpElement.RichTextEditingAllowed.Value = true;
            tmpElement.ResetOnDeactivation.Value = false;
            tmpElement.RestoreOriginalTextOnEscape.Value = false;

            Assert.That(legacyComponent.text, Is.EqualTo("legacy"));
            Assert.That(tmpComponent.text, Is.EqualTo("tmp"));
            Assert.That(legacyCalls, Is.Zero);
            Assert.That(tmpCalls, Is.Zero);
            Assert.That(legacyComponent.contentType, Is.EqualTo(InputField.ContentType.Custom));
            Assert.That(tmpComponent.contentType, Is.EqualTo(TMP_InputField.ContentType.Custom));
            Assert.That(legacyElement.ContentType.Value, Is.EqualTo(InputContentType.Custom));
            Assert.That(tmpElement.ContentType.Value, Is.EqualTo(InputContentType.Custom));
            Assert.That(legacyComponent.lineType, Is.EqualTo(InputField.LineType.MultiLineNewline));
            Assert.That(tmpComponent.lineType, Is.EqualTo(TMP_InputField.LineType.MultiLineNewline));
            Assert.That(tmpComponent.richText, Is.False);
            Assert.That(tmpComponent.isRichTextEditingAllowed, Is.True);
            Assert.That(tmpComponent.resetOnDeActivation, Is.False);
            Assert.That(tmpComponent.restoreOriginalTextOnEscape, Is.False);

            legacyComponent.onValueChanged.Invoke("userlegacy");
            tmpComponent.onValueChanged.Invoke("usertmp");
            Assert.That(legacyElement.Text.Value, Is.EqualTo("userlegacy"));
            Assert.That(tmpElement.Text.Value, Is.EqualTo("usertmp"));
            Assert.That(legacyCalls, Is.EqualTo(1));
            Assert.That(tmpCalls, Is.EqualTo(1));

            runtimeView.Destroy();
            legacyComponent.onValueChanged.Invoke("released");
            tmpComponent.onValueChanged.Invoke("released");
            Assert.That(legacyCalls, Is.EqualTo(1));
            Assert.That(tmpCalls, Is.EqualTo(1));
        }

        [Test]
        public void LegacyAndTmpDropdownsShareOptionsAndValueBehavior()
        {
            root = new GameObject("DropdownView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var legacyObject = new GameObject("LegacyDropdown", typeof(RectTransform), typeof(Dropdown));
            legacyObject.transform.SetParent(root.transform, false);
            var legacyComponent = legacyObject.GetComponent<Dropdown>();
            var legacyElement = legacyObject.AddComponent<LegacyDropdownElement>();
            var tmpObject = new GameObject("TmpDropdown", typeof(RectTransform), typeof(TMP_Dropdown));
            tmpObject.transform.SetParent(root.transform, false);
            var tmpComponent = tmpObject.GetComponent<TMP_Dropdown>();
            var tmpElement = tmpObject.AddComponent<TMPDropdownElement>();
            ((IView)view).Initialize();
            var values = new[] { "One", "Two", "Three" };
            var legacyCalls = 0;
            var tmpCalls = 0;
            legacyElement.OnValueChanged.AddListener(value => legacyCalls++);
            tmpElement.OnValueChanged.AddListener(value => tmpCalls++);

            legacyElement.Options.Value = values;
            tmpElement.Options.Value = values;
            legacyElement.Value.Value = 1;
            tmpElement.Value.Value = 2;
            tmpElement.MultiSelect.Value = true;

            Assert.That(legacyComponent.options.Select(option => option.text), Is.EqualTo(values));
            Assert.That(tmpComponent.options.Select(option => option.text), Is.EqualTo(values));
            Assert.That(legacyComponent.value, Is.EqualTo(1));
            Assert.That(tmpComponent.value, Is.EqualTo(2));
            Assert.That(tmpComponent.MultiSelect, Is.True);
            Assert.That(legacyCalls, Is.Zero);
            Assert.That(tmpCalls, Is.Zero);

            legacyComponent.onValueChanged.Invoke(2);
            tmpComponent.onValueChanged.Invoke(3);
            Assert.That(legacyCalls, Is.EqualTo(1));
            Assert.That(tmpCalls, Is.EqualTo(1));

            legacyElement.Options.Value = null;
            tmpElement.Options.Value = null;
            Assert.That(legacyComponent.options, Is.Empty);
            Assert.That(tmpComponent.options, Is.Empty);
        }

        [Test]
        public void ElementSynchronizesExternalTransformChanges()
        {
            root = new GameObject("TransformView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var child = new GameObject("Target", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var element = child.AddComponent<RectTransformElement>();
            ((IView)view).Initialize();
            var notifications = 0;
            element.LocalPosition.OnValueChanged += (oldValue, newValue) => notifications++;

            child.transform.localPosition = new Vector3(12f, 5f, 0f);
            ((IExternalValueSource)element).SynchronizeExternalValues();

            Assert.That(element.LocalPosition.Value, Is.EqualTo(new Vector3(12f, 5f, 0f)));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(child.transform.hasChanged, Is.False);
        }

        [Test]
        public void RemoveCloneSuffixOnlyRemovesTrailingUnitySuffix()
        {
            Assert.That(Utility.RemoveCloneSuffix("View(Clone)"), Is.EqualTo("View"));
            Assert.That(Utility.RemoveCloneSuffix("View(Clone)Child"), Is.EqualTo("View(Clone)Child"));
            Assert.That(Utility.RemoveCloneSuffix(string.Empty), Is.Empty);
            Assert.That(Utility.RemoveCloneSuffix(null), Is.Empty);
        }
    }
}
