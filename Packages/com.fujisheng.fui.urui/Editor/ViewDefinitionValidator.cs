using System;
using System.Collections.Generic;
using System.Linq;
using global::URUI.Editor.Authoring;
namespace FUI.Rendering.URUI.Editor
{
    /// <summary>
    /// 宿主提供绑定目标的业务能力要求及明确的动态资源声明。
    /// </summary>
    public sealed class ViewValidationCapabilities
    {
        public IReadOnlyDictionary<string, Type> RequiredAdapters
        {
            get;
        }
        public IReadOnlyCollection<string> DynamicImageUris
        {
            get;
        }
        public IReadOnlyCollection<string> ImageEffectNames
        {
            get;
        }
        public ViewValidationCapabilities(IReadOnlyDictionary<string, Type> requiredAdapters = null,
            IReadOnlyCollection<string> dynamicImageUris = null, IReadOnlyCollection<string> imageEffectNames = null)
        {
            ImageEffectNames = imageEffectNames == null ? null : Array.AsReadOnly(imageEffectNames.ToArray());
            RequiredAdapters = new Dictionary<string, Type>(requiredAdapters ?? new Dictionary<string, Type>());
            DynamicImageUris = Array.AsReadOnly((dynamicImageUris ?? Array.Empty<string>()).ToArray());
        }
    }
    public static class ViewDefinitionValidator
    {
        public static ValidationResult Validate(ViewDefinition definition, IViewAssets assets, ViewValidationCapabilities capabilities)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (assets == null)
            {
                throw new ArgumentNullException(nameof(assets));
            }
            if (capabilities == null)
            {
                throw new ArgumentNullException(nameof(capabilities));
            }
            var issues = new List<ValidationIssue>();
            UnityEngine.TextAsset template;
            try
            {
                template = assets.LoadText(definition.HtmlUri);
            }
            catch (Exception exception)
            {
                issues.Add(new ValidationIssue("missing-resource", exception.Message, definition.HtmlUri));
                return new ValidationResult(issues.AsReadOnly());
            }
            if (template == null)
            {
                issues.Add(new ValidationIssue("missing-resource", "作者模板不存在。", definition.HtmlUri));
                return new ValidationResult(issues.AsReadOnly());
            }
            var nodes = Validator.ReadNodes(template.text, definition.HtmlUri, issues, isFragment: false);
            CheckBindingTargets(definition, assets, nodes,
                capabilities.RequiredAdapters.Select(item => (item.Key, item.Value, string.Empty)), issues);
            foreach (var node in nodes)
            {
                if (node.Tag != "img" || string.IsNullOrWhiteSpace(node.Source) || capabilities.DynamicImageUris.Contains(node.Source))
                {
                    continue;
                }
                try
                {
                    using var image = assets.AcquireImage(node.Source);
                    if (image == null)
                    {
                        issues.Add(new ValidationIssue("missing-image", "图片未在资源清单中声明：" + node.Source, definition.HtmlUri, node.Line, node.Id ?? node.Classes));
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(new ValidationIssue("image-resource", exception.Message, definition.HtmlUri, node.Line, node.Id ?? node.Classes));
                }
            }
            var styleSources = new List<KeyValuePair<string, UnityEngine.TextAsset>>();
            try
            {
                foreach (var resource in assets.LoadDependencies(definition.HtmlUri))
                {
                    var path = resource.Source.Split('?', '#')[0];
                    if (!path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    styleSources.Add(new KeyValuePair<string, UnityEngine.TextAsset>(resource.Source, resource.Asset));
                    foreach (var diagnostic in NativeStyleValidator.Validate(resource.Source, resource.Asset))
                    {
                        issues.Add(new ValidationIssue(diagnostic.Code, diagnostic.Message, diagnostic.Uri, diagnostic.Line));
                    }
                }
            }
            catch (Exception exception)
            {
                issues.Add(new ValidationIssue("missing-dependency", exception.Message, definition.HtmlUri));
            }
            foreach (var diagnostic in StyleDependencyValidator.Validate(styleSources, capabilities.ImageEffectNames))
            {
                issues.Add(new ValidationIssue(diagnostic.Code, diagnostic.Message, diagnostic.Uri, diagnostic.Line));
            }
            foreach (var key in definition.ImageGroupKeys)
            {
                try
                {
                    if (assets.LoadImageGroup(key) == null)
                    {
                        issues.Add(new ValidationIssue("missing-image-group", "图片组不存在：" + key, definition.HtmlUri));
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(new ValidationIssue("image-group", exception.Message, definition.HtmlUri));
                }
            }
            return new ValidationResult(issues.AsReadOnly());
        }

        /// <summary>使用生成清单校验作者 HTML 中的业务目标，返回节点和来源成员。</summary>
        public static ValidationResult ValidateBindingTargets(ViewDefinition definition, IViewAssets assets,
            string html, IEnumerable<(string Path, Type Capability, string Source)> required)
        {
            var issues = new List<ValidationIssue>();
            var nodes = Validator.ReadNodes(html, definition.HtmlUri, issues, isFragment: false);
            CheckBindingTargets(definition, assets, nodes, required, issues);
            return new ValidationResult(issues.AsReadOnly());
        }

        static void CheckBindingTargets(ViewDefinition definition, IViewAssets assets, IReadOnlyList<Validator.Node> nodes,
            IEnumerable<(string Path, Type Capability, string Source)> required, List<ValidationIssue> issues)
        {
            IReadOnlyList<ElementBinding> bindings;
            try
            {
                bindings = definition.DescribeBindings(assets);
            }
            catch (Exception exception)
            {
                issues.Add(new ValidationIssue("binding-descriptor", exception.Message, definition.HtmlUri));
                return;
            }
            foreach (var binding in bindings)
            {
                if (binding == null)
                {
                    issues.Add(new ValidationIssue("binding-descriptor", "绑定描述不能为空。", definition.HtmlUri));
                    continue;
                }
                var key = binding.Selector.Substring(1);
                var matches = nodes.Where(node => binding.Selector[0] == '#' ? node.Id == key
                    : (node.Classes ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Contains(key)).ToArray();
                if (matches.Length == 0)
                {
                    issues.Add(new ValidationIssue("binding-missing", "选择器没有匹配节点：" + binding.Selector, definition.HtmlUri, node: binding.Selector));
                }
                else if (binding.Selector[0] == '#' && matches.Length > 1)
                {
                    issues.Add(new ValidationIssue("binding-ambiguous", "局部 id 匹配多个节点：" + binding.Selector, definition.HtmlUri, matches[1].Line, binding.Selector));
                }
            }
            foreach (var (path, capability, source) in required)
            {
                var candidates = bindings.Where(binding => binding != null && binding.Name == path).ToArray();
                if (candidates.Any(binding => capability.IsAssignableFrom(binding.AdapterType)))
                {
                    continue;
                }
                if (candidates.Length == 0)
                {
                    var target = nodes.Where(node => string.Equals(node.Id, path, StringComparison.Ordinal)).ToArray();
                    var id = "#" + path;
                    var detail = string.IsNullOrEmpty(source) ? "" : " · " + source;
                    if (target.Length == 0)
                    {
                        issues.Add(new ValidationIssue("binding-missing", "缺少绑定节点 " + id + detail, definition.HtmlUri, node: id));
                    }
                    else if (target.Length > 1)
                    {
                        issues.Add(new ValidationIssue("binding-ambiguous", "局部 ID 匹配多个节点 " + id + detail, definition.HtmlUri, target[1].Line, id));
                    }
                    else if (!capability.IsAssignableFrom(StandardElementAdapters.GetAuthoredAdapterType(target[0].Tag, target[0].InputType)))
                    {
                        issues.Add(new ValidationIssue("binding-capability", path + " 缺少能力 " + capability.FullName + detail,
                            definition.HtmlUri, target[0].Line, id));
                    }
                    continue;
                }
                var selector = candidates.FirstOrDefault()?.Selector ?? "";
                var node = nodes.FirstOrDefault(candidate => !string.IsNullOrEmpty(selector) && (selector[0] == '#' ? candidate.Id == selector.Substring(1)
                    : (candidate.Classes ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Contains(selector.Substring(1))));
                issues.Add(new ValidationIssue("binding-capability", path + " 缺少能力 " + capability.FullName + (string.IsNullOrEmpty(source) ? "" : " · " + source),
                    definition.HtmlUri, node?.Line ?? 0, selector));
            }
        }
    }
}
