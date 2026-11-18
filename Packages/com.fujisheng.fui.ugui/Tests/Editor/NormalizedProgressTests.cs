using System;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Tests
{
    public sealed class NormalizedProgressTests
    {
        GameObject root;
        [TearDown] public void TearDown() { if (root) Object.DestroyImmediate(root); }
        (IFloatValueElement element, RectTransform rect) Build(bool stretch)
        {
            root = new GameObject("ProgressView", typeof(RectTransform), typeof(View));
            ((RectTransform)root.transform).sizeDelta = new Vector2(400, 80);
            var child = new GameObject("progress", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)child.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = stretch ? new Vector2(0, .5f) : new Vector2(.5f, .5f);
            rect.anchorMax = stretch ? new Vector2(1, .5f) : new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(stretch ? -20 : 200, 20);
            rect.anchoredPosition = new Vector2(15, 10);
            var type = typeof(SliderElement).Assembly.GetType("FUI.Rendering.UGUI.Elements.ProgressElement");
            Assert.IsNotNull(type);
            child.AddComponent(type);
            var view = (IView)root.GetComponent<View>();
            view.Initialize();
            return (view.GetElement<IFloatValueElement>("progress"), rect);
        }
        [Test] public void AuthorRectangleFillsFromStableLeftEdgeEvenWithoutSprite()
        {
            var (element, rect) = Build(false);
            var left = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
            var commands = 0;
            element.ValueChanged.AddListener(_ => commands++);
            element.Value.Value = .25f;
            Assert.That(rect.rect.width, Is.EqualTo(50).Within(.01));
            Assert.That(rect.anchoredPosition.x - rect.rect.width * rect.pivot.x, Is.EqualTo(left).Within(.01));
            element.Value.Value = 1;
            Assert.That(rect.rect.width, Is.EqualTo(200).Within(.01));
            element.Value.Value = 0;
            Assert.That(rect.rect.width, Is.Zero.Within(.01));
            Assert.AreEqual(0, commands);
        }
        [Test] public void StretchedProgressTracksParentResize()
        {
            var (element, rect) = Build(true);
            element.Value.Value = .5f;
            Assert.That(rect.rect.width, Is.EqualTo(190).Within(.01));
            ((RectTransform)root.transform).sizeDelta = new Vector2(600, 80);
            rect.ForceUpdateRectTransforms();
            Assert.That(rect.rect.width, Is.EqualTo(290).Within(.01));
            element.Value.Value = 1;
            Assert.That(rect.rect.width, Is.EqualTo(580).Within(.01));
        }
    }
}
