using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using global::URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    // 真实编译测试源码；正式运行时由项目的 Editor 资产管线提供这些字节。
    internal static class CompiledFixtureAssets
    {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string Library = "__Internal";
#else
        const string Library = "URUIBridge";
#endif
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        static extern int urui_compile_document([MarshalAs(UnmanagedType.LPUTF8Str)] string html,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string uri, int fragment, [In] Resource[] files, int count,
            [Out] byte[] output, int capacity, out int required);
        [StructLayout(LayoutKind.Sequential)]
        struct Resource
        {
            public IntPtr Uri;
            public IntPtr Bytes;
            public int Length;
        }
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        static extern int urui_compile_template([In] byte[] parent, int parentLength,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string html, [MarshalAs(UnmanagedType.LPUTF8Str)] string uri,
            [Out] byte[] output, int capacity, out int required);
        [Serializable]
        sealed class TemplatePacket
        {
            public string sourceUrl;
            public string parentFingerprint;
            public byte[] data;
        }
        [Serializable]
        sealed class Packet
        {
            public string sourceUrl;
            public string fingerprint;
            public int compilerVersion;
            public byte[] data;
            public TextResource[] dependencies;
            public TemplatePacket[] templates;
        }
        static readonly List<CompiledDocumentAsset> owned = new List<CompiledDocumentAsset>();
        internal static CompiledDocumentAsset Create(TextAsset source, string uri, TextResource[] files = null)
        {
            if (!source)
            {
                return null;
            }
            files ??= Array.Empty<TextResource>();
            var resources = new Resource[files.Length];
            var pins = new GCHandle[files.Length];
            try
            {
                for (var i = 0; i < files.Length; i++)
                {
                    var data = files[i].Asset.bytes;
                    pins[i] = GCHandle.Alloc(data, GCHandleType.Pinned);
                    resources[i] = new Resource
                    {
                        Uri = Marshal.StringToCoTaskMemUTF8(files[i].Source),
                        Bytes = pins[i].AddrOfPinnedObject(),
                        Length = data.Length
                    };
                }
                var bytes = new byte[65536];
                var result = urui_compile_document(source.text, uri, 0, resources, resources.Length, bytes, bytes.Length, out var count);
                if (result == -3 && count > bytes.Length)
                {
                    bytes = new byte[count];
                    result = urui_compile_document(source.text, uri, 0, resources, resources.Length, bytes, bytes.Length, out count);
                }
                Assert.AreEqual(1, result, "Fixture must compile before runtime loading.");
                Array.Resize(ref bytes, count);
                var templates = new List<TemplatePacket>();
                foreach (var path in new[] { "URUI/Templates/ReviewDynamic", "URUI/Tests/AuthoredDynamic" })
                {
                    var text = UnityEngine.Resources.Load<TextAsset>(path);
                    if (!text)
                    {
                        continue;
                    }
                    var encoded = new byte[65536];
                    result = urui_compile_template(bytes, bytes.Length, text.text, "/" + path + ".html", encoded, encoded.Length, out count);
                    Assert.AreEqual(1, result);
                    Array.Resize(ref encoded, count);
                    templates.Add(new TemplatePacket
                    {
                        sourceUrl = "/" + path + ".html",
                        parentFingerprint = uri,
                        data = encoded
                    });
                }
                var asset = ScriptableObject.CreateInstance<CompiledDocumentAsset>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Packet
                {
                    sourceUrl = uri,
                    fingerprint = uri,
                    compilerVersion = CompiledDocumentAsset.CompilerVersion,
                    data = bytes,
                    dependencies = files,
                    templates = templates.ToArray()
                }), asset);
                owned.Add(asset);
                return asset;
            }
            finally
            {
                for (var i = 0; i < files.Length; i++)
                {
                    if (pins[i].IsAllocated)
                    {
                        pins[i].Free();
                    }
                    if (resources[i].Uri != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(resources[i].Uri);
                    }
                }
            }
        }
        internal static void Release()
        {
            foreach (var asset in owned)
            {
                if (asset)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }
            owned.Clear();
        }
    }
    [SetUpFixture]
    sealed class CompiledFixtureCleanup
    {
        [OneTimeTearDown]
        public void Release() => CompiledFixtureAssets.Release();
    }
}
