using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using global::URUI.Documents;
using global::URUI.Hosting;
using global::URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    internal static class FuiHtmlFixtureAssets
    {
        internal static Document Create(GameObject root)
        {
            var surface = root.GetComponent<Surface>() ?? root.AddComponent<Surface>();
            surface.Input.UseUnityInput = false;
            var document = root.AddComponent<Document>();
            document.Surface = surface;
            return document;
        }
        static List<TextResource> BaseFiles()
        {
            var files = new List<TextResource> {
                new TextResource("/urui/base.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/base"))
            };
            foreach (var family in new[] { "Serif", "Sans", "Mono" })
                foreach (var face in new[] { "Regular", "Italic", "Bold", "BoldItalic" })
                {
                    var name = "Liberation" + family + "-" + face + ".ttf";
                    files.Add(new TextResource("/urui/Fonts/" + name + ".bytes", global::UnityEngine.Resources.Load<TextAsset>("URUI/Fonts/" + name)));
                }
            return files;
        }
        internal static void Load(Document document, string resource)
        {
            var files = document.TextResources.ToList();
            foreach (var file in BaseFiles()) if (!files.Any(existing => existing.Source == file.Source)) files.Add(file);
            document.TextResources = files.ToArray();
            document.Load(global::UnityEngine.Resources.Load<TextAsset>(resource), resource + ".html");
        }
        internal static void SetStyles(UruiStage stage, params TextAsset[] styles)
        {
            var files = BaseFiles();
            foreach (var asset in styles) files.Add(new TextResource("/fixtures/" + asset.name + ".css", asset));
            stage.TextResources = files.ToArray();
        }
        internal static UruiScope OpenPage(UruiStage stage, string body, FUI.Navigation.Layer layer)
        {
            stage.Surface.Input.UseUnityInput = false;
            var links = string.Concat(stage.TextResources.Where(file => file.Source.EndsWith(".css", System.StringComparison.Ordinal))
                .Select(file => "<link rel='stylesheet' href='" + file.Source + "'>"));
            var html = "<!doctype html><html><head>" + links + "<style>html,body{margin:0;width:100%;height:100%;pointer-events:none;}body>*{pointer-events:auto;}</style></head><body>" + body + "</body></html>";
            var text = new TextAsset(html);
            try { return stage.OpenPage(CompiledFixtureAssets.Create(text, "/fixtures/page.html", stage.TextResources), layer); }
            finally { Object.DestroyImmediate(text); }
        }
    }
}
