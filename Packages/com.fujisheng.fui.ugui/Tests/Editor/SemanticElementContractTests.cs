using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Tests
{
    public sealed class SemanticElementContractTests
    {
        [Test]
        public void SemanticQueriesForwardExistingUguiStateAndUserEvents()
        {
            var root = new GameObject("Semantic Contracts", typeof(RectTransform));
            try
            {
                var view = root.AddComponent<View>();
                var slider = Add<SliderElement>(root, "Volume");
                var toggle = Add<ToggleElement>(root, "Vibration");
                var button = Add<ButtonElement>(root, "Close");
                var label = Add<LegacyTextElement>(root, "Title");
                var runtime = (IView)view;
                runtime.Initialize();

                var value = runtime.GetElement<IFloatValueElement>("Volume");
                var flag = runtime.GetElement<IBoolValueElement>("Vibration");
                var click = runtime.GetElement<IClickElement>("Close");
                var text = runtime.GetElement<ITextElement>("Title");
                Assert.That(value, Is.SameAs(slider));
                Assert.That(flag, Is.SameAs(toggle));
                Assert.That(click, Is.SameAs(button));
                Assert.That(text, Is.SameAs(label));
                Assert.That(value.Value, Is.SameAs(slider.Value));
                Assert.That(value.ValueChanged, Is.SameAs(slider.OnValueChanged));
                Assert.That(flag.Value, Is.SameAs(toggle.IsOn));
                Assert.That(flag.ValueChanged, Is.SameAs(toggle.OnValueChanged));
                Assert.That(click.Click, Is.SameAs(button.OnClick));
                Assert.That(text.Text, Is.SameAs(label.Text));

                var volumeInputs = 0;
                var flagInputs = 0;
                var clicks = 0;
                value.ValueChanged.AddListener(_ => volumeInputs++);
                flag.ValueChanged.AddListener(_ => flagInputs++);
                click.Click.AddListener(() => clicks++);
                slider.GetComponent<Slider>().value = 0.75F;
                toggle.GetComponent<Toggle>().isOn = !toggle.IsOn.Value;
                button.GetComponent<Button>().onClick.Invoke();
                Assert.That(value.Value.Value, Is.EqualTo(0.75F));
                Assert.That(volumeInputs, Is.EqualTo(1));
                Assert.That(flagInputs, Is.EqualTo(1));
                Assert.That(clicks, Is.EqualTo(1));

                value.Value.Value = 0.25F;
                flag.Value.Value = !flag.Value.Value;
                text.Text.Value = "semantic text";
                Assert.That(volumeInputs, Is.EqualTo(1));
                Assert.That(flagInputs, Is.EqualTo(1));
                Assert.That(slider.GetComponent<Slider>().value, Is.EqualTo(0.25F));
                Assert.That(label.GetComponent<Text>().text, Is.EqualTo("semantic text"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        static T Add<T>(GameObject root, string name) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }
    }
}
