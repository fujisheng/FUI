using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Events;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class AuthoredOverflowTests
    {
        GameObject cameraObject;
        GameObject root;
        Document document;
        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            root = new GameObject("Authored overflow document");
            document = FuiHtmlFixtureAssets.Create(root);
            document.Surface.Input.UseUnityInput = false;
            document.TextResources = new[] { new TextResource("URUI/Tests/AuthoredOverflow.styles.css", UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredOverflow.styles")) };
            FuiHtmlFixtureAssets.Load(document, "URUI/Tests/AuthoredOverflow");
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }

        [UnityTest]
        public IEnumerator OrdinaryOverflowDivUsesTouchScrollingInertiaAndCancelsClick()
        {
            yield return null;
            yield return null;
            var scroll = document.Q("scroll");
            var target = document.Q("target");
            var clicks = 0;
            Assert.That(scroll.ScrollSize.y, Is.GreaterThan(600), "测试内容必须有足够滚动范围。");
            using var observer = target.RegisterCallback<ClickEvent>(_ => clicks++);
            var point = new Vector2(80, 120);
            document.Surface.Input.SendPointer(PointerPhase.Down, point, 66, true);
            for (var step = 1; step <= 4; step++)
            {
                yield return new WaitForSecondsRealtime(.04f);
                document.Surface.Input.SendPointer(PointerPhase.Move, point - new Vector2(0, step * 20), 66, true);
            }
            var dragged = scroll.ScrollOffset.y;
            document.Surface.Input.SendPointer(PointerPhase.Up, point - new Vector2(0, 80), 66, true);
            var released = scroll.ScrollOffset.y;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(dragged, Is.GreaterThan(40), "普通 overflow 节点必须接收统一指针 API 的真实触摸默认滚动。");
            Assert.That(scroll.ScrollOffset.y, Is.GreaterThan(released + 1), "松手后应继续原生滚动惯性。");
            Assert.That(clicks, Is.Zero, "滚动超过阈值后不得点击手指下的按钮。");
        }

        [UnityTest]
        public IEnumerator CancelledTouchCanReuseItsPointerWithoutClicking()
        {
            yield return null;
            yield return null;
            var target = document.Q("target");
            var clicks = 0;
            using var observer = target.RegisterCallback<ClickEvent>(_ => clicks++);
            var point = new Vector2(80, 100);
            document.Surface.Input.SendPointer(PointerPhase.Down, point, 18, true);
            document.Surface.Input.SendPointer(PointerPhase.Cancel, point, 18, true);
            Assert.That(clicks, Is.Zero);
            document.Surface.Input.SendPointer(PointerPhase.Down, point, 18, true);
            document.Surface.Input.SendPointer(PointerPhase.Up, point, 18, true);
            Assert.That(clicks, Is.EqualTo(1));
        }
    }
}
