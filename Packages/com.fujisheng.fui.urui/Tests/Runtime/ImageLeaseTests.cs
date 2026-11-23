using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class ImageLeaseTests
    {
        const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly Assembly Runtime = typeof(global::URUI.Documents.Document).Assembly;

        static object Registry()
        {
            return Activator.CreateInstance(Runtime.GetType("URUI.Internal.Resources.BorrowedTextureRegistry"), true);
        }

        static object Lease(Texture texture, Action release)
        {
            var type = Runtime.GetType("URUI.Resources.ImageLease");
            Assert.IsNotNull(type, "按需图片需要显式资源租约，不能只存 Texture 引用");
            return Activator.CreateInstance(type, texture, release);
        }

        static object Call(object registry, string method, params object[] args)
        {
            var target = registry.GetType().GetMethod(method, Internal);
            Assert.IsNotNull(target, method);
            return target.Invoke(registry, args);
        }

        static ulong RegisterResolved(object registry, object lease)
        {
            return (ulong)Call(registry, "RegisterResolved", lease, 0UL);
        }

        static object Pin(object registry, ulong id, out Texture texture)
        {
            var type = registry.GetType().GetNestedType("SubmissionPins", BindingFlags.NonPublic);
            Assert.IsNotNull(type);
            var pins = Activator.CreateInstance(type, true);
            var args = new object[] { registry, id, null };
            Assert.IsTrue((bool)Call(pins, "AcquireOne", args));
            texture = (Texture)args[2];
            return pins;
        }

        [Test]
        public void ResolvedImageWaitsForNativeReleaseAndLastGpuPin()
        {
            var texture = new Texture2D(2, 2);
            var released = 0;
            try
            {
                var registry = Registry();
                var lease = Lease(texture, () => released++);
                var id = RegisterResolved(registry, lease);
                var first = Pin(registry, id, out var borrowed);
                Assert.AreSame(texture, borrowed);
                var second = Pin(registry, id, out _);
                Call(registry, "ReleaseResolved", id);
                Call(registry, "Clear");
                Assert.AreEqual(0, released, "Clear 不能释放 GPU 仍引用的图片");
                Call(first, "Release");
                Call(first, "Release");
                Assert.AreEqual(0, released);
                Call(second, "Release");
                Assert.AreEqual(1, released);
                Call(second, "Release");
                ((IDisposable)lease).Dispose();
                Assert.AreEqual(1, released);
                Assert.IsTrue(texture, "只有显式 release 回调取得释放权，URUI 不自动销毁借用原图");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void IndependentLeasesOfSameTextureEachReleaseOnce()
        {
            var texture = new Texture2D(2, 2);
            var released = 0;
            try
            {
                var registry = Registry();
                var first = RegisterResolved(registry, Lease(texture, () => released++));
                var second = RegisterResolved(registry, Lease(texture, () => released++));
                Assert.AreNotEqual(first, second);
                Call(registry, "ReleaseResolved", first);
                Assert.AreEqual(1, released);
                Call(registry, "Clear");
                Assert.AreEqual(2, released);
                Call(registry, "Clear");
                Assert.AreEqual(2, released);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void LiveRenderTextureRemainsBorrowedUntilNativeAndGpuOwnersRelease()
        {
            var texture = new RenderTexture(32, 16, 0);
            texture.Create();
            var released = 0;
            try
            {
                var registry = Registry();
                var lease = Lease(texture, () => released++);
                var id = RegisterResolved(registry, lease);
                var pin = Pin(registry, id, out var borrowed);
                Assert.AreSame(texture, borrowed, "视频输出必须保持同一 GPU 纹理，不能复制成 Texture2D");
                Call(registry, "ReleaseResolved", id);
                Call(registry, "Clear");
                Assert.AreEqual(0, released);
                Call(pin, "Release");
                Assert.AreEqual(1, released);
                Assert.IsTrue(texture.IsCreated());
            }
            finally
            {
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void RenderTextureMustBeCreatedSampleableAndSingleSample()
        {
            var texture = new RenderTexture(32, 16, 0) { antiAliasing = 2 };
            try
            {
                var registry = Registry();
                Assert.Catch<Exception>(() => Call(registry, "RegisterResolved", Lease(texture, null)));
                texture.Create();
                Assert.Catch<Exception>(() => Call(registry, "RegisterResolved", Lease(texture, null)));
            }
            finally
            {
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void StoppedProducerWaitsForRenderLeasesBeforeReleasingOutput()
        {
            var texture = new Texture2D(2, 2);
            var released = 0;
            try
            {
                var source = new global::URUI.Resources.ImageLeaseSource(texture, () => released++);
                var first = source.Acquire();
                var second = source.Acquire();
                source.Dispose();
                source.Dispose();
                Assert.AreEqual(0, released);
                Assert.Throws<ObjectDisposedException>(() => source.Acquire());
                first.Dispose();
                first.Dispose();
                Assert.AreEqual(0, released);
                second.Dispose();
                Assert.AreEqual(1, released);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
