using System;
using System.Collections.Generic;
using System.Linq;
using FUI.Rendering;
using NUnit.Framework;
using UnityEngine;
using global::URUI.Resources;

namespace FUI.Rendering.URUI.Editor.Tests
{
    public sealed class ViewDefinitionValidatorTests
    {
        sealed class Assets : IViewAssets, IDisposable
        {
            public readonly Dictionary<string, TextAsset> Texts = new Dictionary<string, TextAsset>();
            public Texture2D Texture;
            public int Releases;
            string[] styles = Array.Empty<string>();
            public TextAsset LoadText(string uri) => Texts.TryGetValue(uri, out var text) ? text : null;
            public global::URUI.Resources.CompiledDocumentAsset LoadCompiled(string uri)
                => throw new InvalidOperationException("Metadata validation does not instantiate a runtime page.");
            public ImageLease AcquireImage(string uri)
            {
                if (uri == "ambiguous.png")
                {
                    throw new InvalidOperationException("图片资源键有歧义");
                }
                return uri == "icon.png" && Texture ? new ImageLease(Texture, () => Releases++) : null;
            }
            public IReadOnlyList<ImageAsset> LoadImageGroup(string key) => Array.Empty<ImageAsset>();
            public IReadOnlyList<TextResource> LoadDependencies(string htmlUri) => styles.Select(uri => new TextResource(uri, Texts[uri])).ToArray();
            public void Add(string uri, string text)
            {
                Texts.Add(uri, new TextAsset(text));
            }
            public void SetPage(string body, params string[] dependencies)
            {
                styles = (string[])dependencies.Clone();
                if (Texts.Remove("page.html", out var previous))
                {
                    UnityEngine.Object.DestroyImmediate(previous);
                }
                var links = string.Concat(styles.Select(uri => "<link rel='stylesheet' href='" + uri + "'>"));
                Add("page.html", "<!doctype html><html><head>" + links + "</head><body>" + body + "</body></html>");
            }
            public void Dispose()
            {
                foreach (var text in Texts.Values)
                {
                    UnityEngine.Object.DestroyImmediate(text);
                }
                if (Texture)
                {
                    UnityEngine.Object.DestroyImmediate(Texture);
                }
            }
        }
        [Test]
        public void TopLevelDefinitionRejectsAFragmentAuthorAsset()
        {
            using var assets = new Assets();
            assets.Add("page.html", "<div>Fragment</div>");
            var definition = new ViewDefinition("page", "page.html", _ => Array.Empty<ElementBinding>());
            var result = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities());
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "HTML_DOCTYPE"));
        }
        [Test]
        public void OrdinaryIdsAndRepeatedClassesValidateWithoutCreatingAdapters()
        {
            using var assets = new Assets();
            assets.SetPage("<div id='title'></div><div class='row'></div><div class='row' hidden></div>");
            var created = 0;
            var definition = new ViewDefinition("page", "page.html", _ => new[] {
                ElementBinding.For("Title", "#title", (e, n) => { created++;
return new TextElement(e, n);
}),
                ElementBinding.For("Rows", ".row", (e, n) => new ElementAdapter(e, n)) });
            var result = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities());
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, created);
        }
        [Test]
        public void ComposedCapabilitiesShareOneBusinessName()
        {
            using var assets = new Assets();
            assets.SetPage("<button id='icon'></button>");
            var definition = new ViewDefinition("page", "page.html", _ => new[] {
                ElementBinding.For("Icon", "#icon", (e, n) => new ButtonElement(e, n)),
                ElementBinding.For("Icon", "#icon", (e, n) => new ImageElement(e, n)) });
            var required = new Dictionary<string, Type> { ["Icon"] = typeof(IImageElement) };
            Assert.IsTrue(ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities(required)).IsValid);
        }
        [Test]
        public void MissingAmbiguousAndWrongCapabilitiesHaveUriLineAndSelector()
        {
            using var assets = new Assets();
            assets.SetPage("<div id='same'></div>\n<div id='same'></div>");
            var definition = new ViewDefinition("page", "page.html", _ => new[] {
                ElementBinding.For("Title", "#same", (e, n) => new ButtonElement(e, n)),
                ElementBinding.For("Missing", ".missing", (e, n) => new ElementAdapter(e, n)) });
            var result = ViewDefinitionValidator.Validate(definition, assets,
                new ViewValidationCapabilities(new Dictionary<string, Type> { ["Title"] = typeof(ITextElement) }));
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "binding-ambiguous" && issue.Uri == "page.html" && issue.Line == 2 && issue.Node == "#same"));
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "binding-missing"));
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "binding-capability"));
        }
        [Test]
        public void MissingAndAmbiguousImagesAreLocatedAndValidLeasesAreReleased()
        {
            using var assets = new Assets();
            assets.Texture = new Texture2D(2, 2);
            assets.SetPage("<img src='icon.png'>\n<img src='missing.png'>\n<img src='ambiguous.png'>");
            var definition = new ViewDefinition("page", "page.html", _ => Array.Empty<ElementBinding>());
            var result = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities());
            Assert.AreEqual(1, assets.Releases);
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "missing-image" && issue.Line == 2));
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "image-resource" && issue.Line == 3));
        }
        [Test]
        public void NativeParserAcceptsThemeVariablesAndLocatesUnsupportedDeclarations()
        {
            using var assets = new Assets();
            assets.SetPage("<div id='title'></div>", "theme.css");
            assets.Add("theme.css", ".card { --theme:blue; opacity:.5; }");
            var definition = new ViewDefinition("page", "page.html", _ => Array.Empty<ElementBinding>());
            var valid = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities());
            Assert.IsTrue(valid.IsValid, valid.Issues.Count == 0 ? "" : valid.Issues[0].Message);
            assets.Add("invalid.css", ".card { unregistered-property:4px; }");
            assets.SetPage("<div id='title'></div>", "invalid.css");
            Assert.That(ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities()).Issues,
                Has.Some.Matches<ValidationIssue>(issue => issue.Code == "CSS_PROPERTY_VALUE" && issue.Uri == "invalid.css" && issue.Line > 0));
        }

        [Test]
        public void KeyframeOverridesAndDocumentSelectorsStayStandardAcrossDependencies()
        {
            using var assets = new Assets();
            assets.SetPage("<div class='card'></div>", "a.css", "b.css");
            assets.Add("a.css", global::UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/StyleConflictA").text);
            assets.Add("b.css", global::UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/StyleConflictB").text);
            var definition = new ViewDefinition("page", "page.html", _ => Array.Empty<ElementBinding>());
            var result = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities());
            Assert.IsTrue(result.IsValid, string.Join(";", result.Issues.Select(issue => issue.Message)));
            result = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities(imageEffectNames: Array.Empty<string>()));
            Assert.That(result.Issues, Has.Some.Matches<ValidationIssue>(issue => issue.Code == "unknown-effect" && issue.Line == 3));
        }
        [Test]
        public void ConditionalBlocksAllowDocumentSelectorsAndKeyframeOverrides()
        {
            using var assets = new Assets();
            assets.SetPage("<div class='card'></div>", "a.css", "media.css");
            assets.Add("a.css", global::UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/StyleConflictA").text);
            assets.Add("media.css", global::UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/StyleMediaConflict").text);
            var definition = new ViewDefinition("page", "page.html", _ => Array.Empty<ElementBinding>());
            var result = ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities());
            Assert.IsTrue(result.IsValid, string.Join(";", result.Issues.Select(issue => issue.Message)));
        }
        [Test]
        public void MissingTemplateAndDescriptorDomAccessAreReported()
        {
            using var assets = new Assets();
            var definition = new ViewDefinition("page", "page.html", _ => Array.Empty<ElementBinding>());
            Assert.That(ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities()).Issues,
                Has.Some.Matches<ValidationIssue>(issue => issue.Code == "missing-resource"));
            assets.SetPage("<div id='title'></div>");
            definition = new ViewDefinition("page", "page.html", context =>
            {
                _ = context.Scope.Root;
                return Array.Empty<ElementBinding>();
            });
            Assert.That(ViewDefinitionValidator.Validate(definition, assets, new ViewValidationCapabilities()).Issues,
                Has.Some.Matches<ValidationIssue>(issue => issue.Code == "binding-descriptor"));
        }
    }
}
