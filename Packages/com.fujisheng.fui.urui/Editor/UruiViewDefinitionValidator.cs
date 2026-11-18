using System;
using System.Collections.Generic;
using System.Linq;
using global::URUI.Editor.Authoring;
namespace FUI.Rendering.URUI.Editor
{
    /// <summary>宿主提供绑定目标的业务能力要求及明确的动态资源声明。</summary>
    public sealed class UruiViewValidationCapabilities
    {
        public IReadOnlyDictionary<string, Type> RequiredAdapters { get; }
        public IReadOnlyCollection<string> DynamicImageUris { get; }
        public IReadOnlyCollection<string> ImageEffectNames {get;}
        public UruiViewValidationCapabilities(IReadOnlyDictionary<string, Type> requiredAdapters = null,
            IReadOnlyCollection<string> dynamicImageUris = null,IReadOnlyCollection<string> imageEffectNames = null)
        {
            ImageEffectNames=imageEffectNames==null?null:Array.AsReadOnly(imageEffectNames.ToArray());
            RequiredAdapters = new Dictionary<string, Type>(requiredAdapters ?? new Dictionary<string, Type>());
            DynamicImageUris = Array.AsReadOnly((dynamicImageUris ?? Array.Empty<string>()).ToArray());
        }
    }
    public static class UruiViewDefinitionValidator
    {
        public static UruiValidationResult Validate(UruiViewDefinition definition, IUruiViewAssets assets, UruiViewValidationCapabilities capabilities)
        {
            if (definition == null) { throw new ArgumentNullException(nameof(definition)); }
            if (assets == null) { throw new ArgumentNullException(nameof(assets)); }
            if (capabilities == null) { throw new ArgumentNullException(nameof(capabilities)); }
            var issues = new List<UruiValidationIssue>();
            UnityEngine.TextAsset template;
            try { template = assets.LoadText(definition.HtmlUri); }
            catch (Exception exception)
            { issues.Add(new UruiValidationIssue("missing-resource", exception.Message, definition.HtmlUri)); return new UruiValidationResult(issues.AsReadOnly()); }
            if (template == null)
            { issues.Add(new UruiValidationIssue("missing-resource", "作者模板不存在。", definition.HtmlUri)); return new UruiValidationResult(issues.AsReadOnly()); }
            var nodes = UruiValidator.ReadNodes(template.text, definition.HtmlUri, issues, isFragment: false);
            IReadOnlyList<UruiElementBinding> bindings;
            try { bindings = definition.DescribeBindings(assets); }
            catch (Exception exception)
            { issues.Add(new UruiValidationIssue("binding-descriptor", exception.Message, definition.HtmlUri)); return new UruiValidationResult(issues.AsReadOnly()); }
            foreach (var binding in bindings)
            {
                if (binding == null) { issues.Add(new UruiValidationIssue("binding-descriptor", "绑定描述不能为空。", definition.HtmlUri)); continue; }
                var key = binding.Selector.Substring(1);
                var matches = nodes.Where(node => binding.Selector[0] == '#' ? node.Id == key
                    : (node.Classes ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Contains(key)).ToArray();
                if (matches.Length == 0)
                { issues.Add(new UruiValidationIssue("binding-missing", "选择器没有匹配节点：" + binding.Selector, definition.HtmlUri, node: binding.Selector)); }
                else if (binding.Selector[0] == '#' && matches.Length > 1)
                { issues.Add(new UruiValidationIssue("binding-ambiguous", "局部 id 匹配多个节点：" + binding.Selector, definition.HtmlUri, matches[1].Line, binding.Selector)); }
            }
            foreach (var requirement in capabilities.RequiredAdapters)
            {
                var candidates = bindings.Where(binding => binding != null && binding.Name == requirement.Key).ToArray();
                if (candidates.Any(binding => requirement.Value.IsAssignableFrom(binding.AdapterType))) { continue; }
                var selector = candidates.FirstOrDefault()?.Selector ?? "";
                var node = nodes.FirstOrDefault(candidate => !string.IsNullOrEmpty(selector) && (selector[0] == '#' ? candidate.Id == selector.Substring(1)
                    : (candidate.Classes ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Contains(selector.Substring(1))));
                issues.Add(new UruiValidationIssue("binding-capability", requirement.Key + " 缺少能力 " + requirement.Value.FullName,
                    definition.HtmlUri, node?.Line ?? 0, selector));
            }
            foreach (var node in nodes)
            {
                if (node.Tag != "img" || string.IsNullOrWhiteSpace(node.Source) || capabilities.DynamicImageUris.Contains(node.Source)) { continue; }
                try
                {
                    using var image = assets.AcquireImage(node.Source);
                    if (image == null) { issues.Add(new UruiValidationIssue("missing-image", "图片未在资源清单中声明：" + node.Source, definition.HtmlUri, node.Line, node.Id ?? node.Classes)); }
                }
                catch (Exception exception)
                { issues.Add(new UruiValidationIssue("image-resource", exception.Message, definition.HtmlUri, node.Line, node.Id ?? node.Classes)); }
            }
            var styleSources=new List<KeyValuePair<string,UnityEngine.TextAsset>>();
            try {
                foreach (var resource in assets.LoadDependencies(definition.HtmlUri)) {
                    var path = resource.Source.Split('?', '#')[0];
                    if (!path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) continue;
                    styleSources.Add(new KeyValuePair<string,UnityEngine.TextAsset>(resource.Source, resource.Asset));
                    foreach (var diagnostic in NativeStyleValidator.Validate(resource.Source, resource.Asset))
                        issues.Add(new UruiValidationIssue(diagnostic.Code, diagnostic.Message, diagnostic.Uri, diagnostic.Line));
                }
            } catch (Exception exception) {
                issues.Add(new UruiValidationIssue("missing-dependency", exception.Message, definition.HtmlUri));
            }
            foreach(var diagnostic in StyleDependencyValidator.Validate(styleSources,capabilities.ImageEffectNames))
            {issues.Add(new UruiValidationIssue(diagnostic.Code,diagnostic.Message,diagnostic.Uri,diagnostic.Line));}
            foreach (var key in definition.ImageGroupKeys)
            {
                try
                { if (assets.LoadImageGroup(key) == null) { issues.Add(new UruiValidationIssue("missing-image-group", "图片组不存在：" + key, definition.HtmlUri)); } }
                catch (Exception exception) { issues.Add(new UruiValidationIssue("image-group", exception.Message, definition.HtmlUri)); }
            }
            return new UruiValidationResult(issues.AsReadOnly());
        }
    }
}
