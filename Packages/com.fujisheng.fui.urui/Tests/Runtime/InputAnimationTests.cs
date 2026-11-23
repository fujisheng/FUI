using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Animation;
using URUI.Controls;
using URUI.Documents;
using URUI.Events;
using URUI.Resources;
namespace FUI.Rendering.URUI.Tests
{
    public sealed class InputAnimationTests
    {
        sealed class TestClock : IUIClock
        {
            public double Now
            {
                get;
                set;
            }
            public float Delta => 0;
        }

        GameObject cameraObject;
        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
        }
        [UnityTest]
        public IEnumerator InputAdapterWritesSilentlyAndCommitsUserEventsOnce()
        {
            var root = new GameObject("Input adapter test");
            try
            {
                var document = Load(root);
                var field = document.Q<TextField>("editor");
                using var adapter = new InputFieldElement(field, "input");
                var changed = 0;
                var submitted = 0;
                var ended = 0;
                adapter.ValueChanged.AddListener(_ => changed++);
                adapter.Submit.AddListener(_ => submitted++);
                adapter.EndEdit.AddListener(_ => ended++);
                adapter.Text.Value = "";
                adapter.CharacterLimit.Value = 3;
                adapter.CaretWidth.Value = 4;
                adapter.CaretBlinkRate.Value = 0;
                yield return null;
                yield return null;
                Assert.AreEqual(4, field.CaretWidth);
                Assert.AreEqual(0, field.CaretBlinkRate);
                Assert.AreEqual(0, changed);
                field.Focus();
                document.Surface.Input.SendText("abcdef");
                Assert.AreEqual("abc", adapter.Text.Value);
                Assert.AreEqual(1, changed);
                document.Surface.Input.SendKey(KeyCode.Return, true);
                document.Surface.Input.SendKey(KeyCode.Return, false);
                Assert.AreEqual(1, submitted);
                Assert.AreEqual(1, ended);
                field.Blur();
                Assert.AreEqual(1, ended);
                adapter.ReadOnly.Value = true;
                field.Focus();
                document.Surface.Input.SendText("z");
                Assert.AreEqual("abc", adapter.Text.Value);
                Assert.AreEqual(1, changed);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        [UnityTest]
        public IEnumerator PreparedTransitionFlushesLayoutBeforeStartingAnimation()
        {
            var root = new GameObject("Prepared CSS transition test");
            try
            {
                var document = Load(root);
                var clock = document.Q("clock");
                using var transition = new ClassTransition(clock, "running", clock, "preparing");
                var operation = transition.PlayAsync(CancellationToken.None).AsTask();
                yield return null;
                Assert.IsTrue(clock.ClassListContains("preparing"));
                Assert.IsFalse(clock.ClassListContains("running"));
                yield return null;
                Assert.IsTrue(clock.ClassListContains("running"));
                var until = Time.realtimeSinceStartup + 2;
                while (!operation.IsCompleted && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                }
                Assert.IsTrue(operation.IsCompletedSuccessfully);
                Assert.IsFalse(clock.ClassListContains("preparing"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator CompletedExitKeepsAuthoredStateUntilStopped()
        {
            var root = new GameObject("Persistent exit state test");
            try
            {
                var document = Load(root);
                var clock = document.Q("clock");
                using var transition = new ClassTransition(clock, "running", clock, null, "finished");
                var operation = transition.PlayAsync(CancellationToken.None).AsTask();
                var until = Time.realtimeSinceStartup + 2;
                while (!operation.IsCompleted && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                }
                Assert.IsTrue(operation.IsCompletedSuccessfully);
                yield return null;
                Assert.IsTrue(clock.ClassListContains("finished"));
                Assert.IsFalse(transition.IsPlaying);
                Assert.AreEqual("0", clock.ResolvedStyle.Get("opacity"));
                transition.Stop();
                Assert.IsFalse(clock.ClassListContains("finished"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        [UnityTest]
        public IEnumerator ExitDoesNotDrawThePopupOnItsAnimationEndFrame()
        {
            var root = new GameObject("CSS exit frame test");
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var output = new RenderTexture(Screen.width, Screen.height, 0);
            var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            camera.targetTexture = output;
            try
            {
                var time = new TestClock();
                var document = Load(root, time);
                var host = document.Q("clock");
                var panel = document.Q("exit-panel");
                var animationEnded = false;
                using var ended = panel.RegisterCallback<AnimationEndEvent>(_ => animationEnded = true);
                using var transition = new ClassTransition(host, "closing", panel, completionClassName: "closed");

                yield return null;
                Assert.That(RenderPanel(), Is.GreaterThan(.9f), "弹窗打开时应当可见。");
                var operation = transition.PlayAsync(CancellationToken.None).AsTask();
                yield return null;
                time.Now = .075;
                yield return null;
                Assert.That(RenderPanel(), Is.InRange(.05f, .75f), "退场中应当仍能看到正在淡出的内容。");

                time.Now = .125;
                yield return null;
                Assert.IsTrue(animationEnded, "必须检查实际收到结束事件的帧。");
                Assert.That(RenderPanel(), Is.LessThan(.02f), "结束帧不能绘制退场前的弹窗。");
                yield return null;
                Assert.IsTrue(operation.IsCompletedSuccessfully);
                Assert.That(RenderPanel(), Is.LessThan(.02f), "过渡结束后仍应保持作者终态。");
                transition.Stop();
                yield return null;
                Assert.That(RenderPanel(), Is.GreaterThan(.9f), "重新打开前应当能清除退场终态。");
            }
            finally
            {
                Object.DestroyImmediate(root);
                camera.targetTexture = null;
                output.Release();
                Object.DestroyImmediate(output);
                Object.DestroyImmediate(pixel);
            }

            float RenderPanel()
            {
                camera.Render();
                var original = RenderTexture.active;
                try
                {
                    RenderTexture.active = output;
                    pixel.ReadPixels(new Rect(48, output.height - 105, 1, 1), 0, 0);
                    return pixel.GetPixel(0, 0).r;
                }
                finally
                {
                    RenderTexture.active = original;
                }
            }
        }

        [UnityTest]
        public IEnumerator ClassTransitionUsesNativeCompletionAndCancelsWithoutLateCompletion()
        {
            var root = new GameObject("CSS transition test");
            try
            {
                var document = Load(root);
                var clock = document.Q("clock");
                clock.SetEnabled(false);
                using var transition = new ClassTransition(clock, "running", clock);
                var operation = transition.PlayAsync(CancellationToken.None).AsTask();
                var until = Time.realtimeSinceStartup + 2;
                while (!operation.IsCompleted && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                }
                Assert.IsTrue(operation.IsCompletedSuccessfully);
                Assert.IsFalse(transition.IsPlaying);
                Assert.IsFalse(clock.ClassListContains("running"));
                yield return null;
                using var cancellation = new CancellationTokenSource();
                operation = transition.PlayAsync(cancellation.Token).AsTask();
                cancellation.Cancel();
                Assert.IsTrue(operation.IsCanceled);
                Assert.IsFalse(transition.IsPlaying);
                yield return null;
                Assert.IsFalse(clock.ClassListContains("running"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        [UnityTest]
        public IEnumerator ResolvedAnimationTargetUsesPreparedChildrenAndIgnoresEarlierEnd()
        {
            var root = new GameObject("Resolved animation completion target");
            try
            {
                var document = FuiHtmlFixtureAssets.Create(root);
                document.TextResources = new[] { new TextResource("URUI/Tests/TransitionTargets.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/TransitionTargets.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/TransitionTargets");
                var scope = document.Q("scope");
                var target = document.Q("first");
                using var transition = new ClassTransition(scope, "running", scope, "preparing", null, () => target);
                var operation = transition.PlayAsync(CancellationToken.None).AsTask();
                yield return null;
                target = document.Q("last");
                yield return new WaitForSecondsRealtime(.12f);
                Assert.IsFalse(operation.IsCompleted, "较早条目的动画完成不能解锁整组输入。");
                var until = Time.realtimeSinceStartup + 1;
                while (!operation.IsCompleted && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                }
                Assert.IsTrue(operation.IsCompletedSuccessfully);
                Assert.IsFalse(scope.ClassListContains("running"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Document Load(GameObject root, IUIClock time = null)
        {
            var document = FuiHtmlFixtureAssets.Create(root);
            if (time != null)
            {
                document.Surface.UIClock = time;
            }
            document.TextResources = new[] { new TextResource("URUI/Tests/InputAnimation.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/InputAnimation.styles")) };
            FuiHtmlFixtureAssets.Load(document, "URUI/Tests/InputAnimation");
            return document;
        }
    }
}
