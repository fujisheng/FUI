using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Samples.Settings.Tests
{
    public sealed class SettingsSampleTests
    {
        [UnityTest]
        public IEnumerator BootstrapOpensGeneratedRouteAndBuildsControls()
        {
            var bootstrap = new GameObject("Settings Sample Test Bootstrap");
            bootstrap.AddComponent<SettingsSampleBootstrap>();

            yield return null;

            Assert.That(Object.FindAnyObjectByType<Canvas>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Slider>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Toggle>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Button>(), Is.Not.Null);

            var slider = Object.FindAnyObjectByType<Slider>();
            var toggle = Object.FindAnyObjectByType<Toggle>();
            var button = Object.FindAnyObjectByType<Button>();
            var status = GameObject.Find("Status").GetComponent<Text>();

            slider.value = 0.75F;
            Assert.That(status.text, Is.EqualTo("Volume: 0.75"));

            toggle.isOn = false;
            Assert.That(status.text, Is.EqualTo("Vibration disabled"));

            button.onClick.Invoke();
            yield return null;
            Assert.That(Object.FindAnyObjectByType<Canvas>(), Is.Null);

            Object.Destroy(bootstrap);
            yield return null;
        }
    }
}
