using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FUI.Editor.Inspection;
using FUI.Infrastructure.Binding;
using FUI.Navigation;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FUI.Editor.Validation
{
    /// <summary>
    /// 验证 View Prefab 的根组件、Element 唯一性、嵌套边界和生成绑定目标。
    /// </summary>
    public static class ViewValidator
    {
        /// <summary>
        /// 根据 Route 资源键在 AssetDatabase 中精确查找同名 Prefab。
        /// </summary>
        sealed class DefaultViewAssetResolver : IViewAssetResolver
        {
            public bool TryResolve(Route route, out string assetPath)
            {
                var assetName = Path.GetFileNameWithoutExtension(route.AssetKey);
                var candidates = AssetDatabase.FindAssets($"{assetName} t:Prefab")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        assetName,
                        StringComparison.Ordinal))
                    .ToArray();
                assetPath = candidates.Length == 1 ? candidates[0] : null;
                return assetPath != null;
            }
        }

        /// <summary>
        /// 保存验证过程中发现的 Element 组件及其绑定名称。
        /// </summary>
        sealed class ElementRecord
        {
            public MonoBehaviour Component;
            public string Name;
        }

        /// <summary>验证所有可发现 Route 和独立 View Prefab。</summary>
        public static IReadOnlyList<ValidationIssue> ValidateAll(IViewAssetResolver resolver = null)
        {
            resolver ??= new DefaultViewAssetResolver();
            var issues = new List<ValidationIssue>();
            var validatedPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var route in DiscoverRoutes())
            {
                if (!resolver.TryResolve(route, out var assetPath))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        route.AssetKey,
                        $"Route {route} could not be resolved to exactly one prefab."));
                    continue;
                }

                validatedPaths.Add(assetPath);
                issues.AddRange(ValidatePrefab(assetPath, route));
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (validatedPaths.Contains(assetPath))
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null && prefab.GetComponent<View>() != null)
                {
                    issues.AddRange(ValidatePrefab(assetPath));
                }
            }

            return DistinctIssues(issues);
        }

        /// <summary>验证指定 View Prefab。</summary>
        public static IReadOnlyList<ValidationIssue> ValidatePrefab(string assetPath)
        {
            return ValidatePrefab(assetPath, null);
        }

        static IReadOnlyList<ValidationIssue> ValidatePrefab(string assetPath, Route route)
        {
            var issues = new List<ValidationIssue>();
            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                var view = root.GetComponent<View>();
                if (view == null)
                {
                    issues.Add(Error(assetPath, "The prefab root is missing a View component.", root));
                    return DistinctIssues(issues);
                }

                if (route != null)
                {
                    ValidateStructure(assetPath, root, issues);
                }
                var allElements = CollectElements(root, stopAtNestedBoundary: false);
                var reachableElements = CollectElements(root, stopAtNestedBoundary: true);
                ValidateDuplicates(assetPath, reachableElements, issues);
                ValidateNestedViews(assetPath, root, reachableElements, issues);
                ValidateElementConfiguration(assetPath, allElements, issues);

                var viewName = route == null
                    ? root.name
                    : Path.GetFileNameWithoutExtension(route.AssetKey);
                var manifests = route == null
                    ? BindingCatalog.Instance.GetManifests(viewName)
                    : ToList(BindingCatalog.Instance.GetManifest(route.ViewModelType, viewName));

                if (route != null && manifests.Count == 0)
                {
                    issues.Add(Error(
                        assetPath,
                        $"No BindingManifest was found for {route.ViewModelType.FullName} and {viewName}.",
                        root));
                }

                foreach (var manifest in manifests)
                {
                    ValidateManifest(assetPath, manifest, allElements, reachableElements, issues);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return DistinctIssues(issues);
        }

        static void ValidateStructure(string assetPath, GameObject root, List<ValidationIssue> issues)
        {
            if (root.GetComponent<Canvas>() == null)
            {
                issues.Add(Error(assetPath, "The prefab root is missing a Canvas component.", root));
            }
            if (root.GetComponent<CanvasGroup>() == null)
            {
                issues.Add(Error(assetPath, "The prefab root is missing a CanvasGroup component.", root));
            }
            if (root.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                issues.Add(Error(assetPath, "The prefab root is missing a GraphicRaycaster component.", root));
            }
            if (root.GetComponentInChildren<EventSystem>(true) != null)
            {
                issues.Add(Error(assetPath, "A View prefab must not contain an EventSystem.", root));
            }
        }

        static void ValidateDuplicates(
            string assetPath,
            IReadOnlyList<ElementRecord> elements,
            List<ValidationIssue> issues)
        {
            foreach (var group in elements.GroupBy(item => (item.Name, item.Component.GetType())))
            {
                if (group.Count() <= 1)
                {
                    continue;
                }

                var first = group.First();
                issues.Add(Error(
                    assetPath,
                    $"Duplicate element name and type: {group.Key.Name} ({group.Key.Item2.FullName}).",
                    first.Component));
            }
        }

        static void ValidateElementConfiguration(
            string assetPath,
            IReadOnlyList<ElementRecord> elements,
            List<ValidationIssue> issues)
        {
            foreach (var record in elements)
            {
                if (record.Component is not Element element)
                {
                    continue;
                }

                var requiredType = ResolveComponentElementType(element.GetType());
                if (requiredType != null && element.GetComponent(requiredType) == null)
                {
                    issues.Add(Error(
                        assetPath,
                        $"Element '{record.Name}' requires component {requiredType.FullName}.",
                        element));
                }

                if (element is TextElement textElement)
                {
                    ValidateTextElement(assetPath, textElement, issues);
                }
                if (element is InputFieldElement inputFieldElement)
                {
                    ValidateInputFieldElement(assetPath, inputFieldElement, issues);
                }
                if (element is DropdownElement dropdownElement)
                {
                    ValidateDropdownElement(assetPath, dropdownElement, issues);
                }
                if (element is RecyclingListElement recyclingList)
                {
                    ValidateRecyclingList(assetPath, recyclingList, issues);
                }
                if (element is SlotListElement slotList)
                {
                    var serialized = new SerializedObject(slotList);
                    if (serialized.FindProperty("content")?.objectReferenceValue == null)
                    {
                        issues.Add(Error(assetPath, $"SlotListElement '{record.Name}' requires Content.", slotList));
                    }
                }
                if (element is AnchoredListElement anchoredList)
                {
                    var serialized = new SerializedObject(anchoredList);
                    var anchors = serialized.FindProperty("anchors");
                    var hasSerializedAnchor = anchors != null && anchors.arraySize > 0;
                    if (!hasSerializedAnchor && anchoredList.transform.childCount == 0)
                    {
                        issues.Add(Error(assetPath, $"AnchoredListElement '{record.Name}' requires at least one anchor.", anchoredList));
                    }
                }
                if (element is ScrollListElement scrollList)
                {
                    var scrollRect = scrollList.GetComponent<ScrollRect>();
                    if (scrollRect == null || scrollRect.content == null)
                    {
                        issues.Add(Error(assetPath, $"ScrollListElement '{record.Name}' requires ScrollRect.content.", scrollList));
                    }
                }
                if (element is ScrollViewElement scrollView)
                {
                    var scrollRect = scrollView.GetComponent<ScrollRect>();
                    if (scrollRect == null || scrollRect.content == null)
                    {
                        issues.Add(Error(assetPath, $"ScrollViewElement '{record.Name}' requires ScrollRect.content.", scrollView));
                    }
                }
            }
        }

        static Type ResolveComponentElementType(Type elementType)
        {
            for (var current = elementType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                {
                    continue;
                }

                var definition = current.GetGenericTypeDefinition();
                if (definition == typeof(ComponentElement<>)
                    || definition == typeof(GraphicElement<>)
                    || definition == typeof(SelectableElement<>)
                    || definition == typeof(TextElement<>)
                    || definition == typeof(InputFieldElement<>)
                    || definition == typeof(DropdownElement<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }
            return null;
        }

        static void ValidateTextElement(string assetPath, TextElement element, List<ValidationIssue> issues)
        {
            var legacyTextCount = element.GetComponents<Text>().Length;
            var tmpTextCount = element.GetComponents<TextMeshProUGUI>().Length;
            if (HasMultipleTextBackends(legacyTextCount, tmpTextCount))
            {
                issues.Add(Error(
                    assetPath,
                    $"TextElement '{element.name}' must have exactly one text rendering backend.",
                    element));
            }

            if (element is LegacyTextElement && legacyTextCount != 1)
            {
                issues.Add(Error(assetPath, $"LegacyTextElement '{element.name}' requires exactly one Text component.", element));
            }
            if (element is TMPTextElement && tmpTextCount != 1)
            {
                issues.Add(Error(assetPath, $"TMPTextElement '{element.name}' requires exactly one TextMeshProUGUI component.", element));
            }
        }

        /// <summary>判断同一对象上的文本渲染后端是否存在冲突。</summary>
        internal static bool HasMultipleTextBackends(int legacyTextCount, int tmpTextCount)
        {
            return legacyTextCount + tmpTextCount > 1;
        }

        static void ValidateInputFieldElement(
            string assetPath,
            InputFieldElement element,
            List<ValidationIssue> issues)
        {
            if (element is LegacyInputFieldElement)
            {
                var component = element.GetComponent<InputField>();
                if (component == null || component.textComponent == null)
                {
                    issues.Add(Error(
                        assetPath,
                        $"LegacyInputFieldElement '{element.name}' requires InputField.textComponent.",
                        element));
                }
            }
            else if (element is TMPInputFieldElement)
            {
                var component = element.GetComponent<TMP_InputField>();
                if (component == null || component.textComponent == null || component.textViewport == null)
                {
                    issues.Add(Error(
                        assetPath,
                        $"TMPInputFieldElement '{element.name}' requires TMP_InputField.textComponent and textViewport.",
                        element));
                }
            }
        }

        static void ValidateDropdownElement(
            string assetPath,
            DropdownElement element,
            List<ValidationIssue> issues)
        {
            if (element is LegacyDropdownElement)
            {
                var component = element.GetComponent<Dropdown>();
                if (component == null || component.template == null || component.captionText == null || component.itemText == null)
                {
                    issues.Add(Error(
                        assetPath,
                        $"LegacyDropdownElement '{element.name}' requires template, captionText and itemText.",
                        element));
                }
            }
            else if (element is TMPDropdownElement)
            {
                var component = element.GetComponent<TMP_Dropdown>();
                if (component == null || component.template == null || component.captionText == null || component.itemText == null)
                {
                    issues.Add(Error(
                        assetPath,
                        $"TMPDropdownElement '{element.name}' requires template, captionText and itemText.",
                        element));
                }
            }
        }

        static void ValidateRecyclingList(
            string assetPath,
            RecyclingListElement element,
            List<ValidationIssue> issues)
        {
            var serialized = new SerializedObject(element);
            var prefab = serialized.FindProperty("itemPrefab")?.objectReferenceValue;
            if (prefab == null && element.transform.childCount == 0)
            {
                issues.Add(Error(assetPath, $"RecyclingListElement '{element.name}' requires an item prefab.", element));
            }
        }

        static void ValidateNestedViews(
            string assetPath,
            GameObject root,
            IReadOnlyList<ElementRecord> reachableElements,
            List<ValidationIssue> issues)
        {
            foreach (var nestedView in reachableElements
                         .Select(record => record.Component)
                         .OfType<View>()
                         .Distinct())
            {
                if (nestedView.gameObject == root)
                {
                    continue;
                }

                if (nestedView.GetComponents<MonoBehaviour>().Any(component => component is IContainerElement))
                {
                    continue;
                }

                issues.Add(Error(
                    assetPath,
                    $"Nested View '{nestedView.name}' must be isolated by an Element that implements IContainerElement.",
                    nestedView));
            }
        }

        static void ValidateManifest(
            string assetPath,
            BindingManifest manifest,
            IReadOnlyList<ElementRecord> allElements,
            IReadOnlyList<ElementRecord> reachableElements,
            List<ValidationIssue> issues)
        {
            foreach (var binding in manifest.properties)
            {
                ValidateTarget(
                    assetPath,
                    manifest,
                    binding.targetInfo.path,
                    binding.targetInfo.type,
                    binding.targetInfo.propertyName,
                    allElements,
                    reachableElements,
                    issues);
            }

            foreach (var command in manifest.commands)
            {
                ValidateTarget(
                    assetPath,
                    manifest,
                    command.targetInfo.path,
                    command.targetInfo.type,
                    command.targetInfo.propertyName,
                    allElements,
                    reachableElements,
                    issues);
            }
        }

        static void ValidateTarget(
            string assetPath,
            BindingManifest manifest,
            string targetName,
            string targetTypeName,
            string memberName,
            IReadOnlyList<ElementRecord> allElements,
            IReadOnlyList<ElementRecord> reachableElements,
            List<ValidationIssue> issues)
        {
            var expectedType = ResolveType(targetTypeName);
            if (expectedType == null)
            {
                issues.Add(Error(assetPath, $"Binding target type could not be loaded: {targetTypeName}."));
                return;
            }

            var namedReachable = reachableElements.Where(item => item.Name == targetName).ToArray();
            // 与运行时一致：精确类型键优先，只有没有精确类型时才退化到可赋值类型。
            var exactMatches = namedReachable
                .Where(item => item.Component.GetType() == expectedType)
                .ToArray();
            var matches = exactMatches.Length > 0
                ? exactMatches
                : namedReachable
                    .Where(item => expectedType.IsAssignableFrom(item.Component.GetType()))
                    .ToArray();
            if (matches.Length == 0)
            {
                var existsBeyondBoundary = allElements.Any(item =>
                    item.Name == targetName && expectedType.IsAssignableFrom(item.Component.GetType()));
                if (existsBeyondBoundary)
                {
                    issues.Add(Error(
                        assetPath,
                        $"Target '{targetName}' for {manifest.viewModelType} is outside the nested View boundary."));
                }
                else if (namedReachable.Length > 0)
                {
                    var actualTypes = string.Join(", ", namedReachable.Select(item => item.Component.GetType().FullName));
                    issues.Add(Error(
                        assetPath,
                        $"Target '{targetName}' for {manifest.viewModelType} has an invalid type. Expected {targetTypeName}; found {actualTypes}.",
                        namedReachable[0].Component));
                }
                else
                {
                    issues.Add(Error(
                        assetPath,
                        $"{manifest.viewModelType} is missing Element '{targetName}' ({targetTypeName})."));
                }
                return;
            }

            var matchedObjects = matches
                .Select(item => item.Component.gameObject)
                .Distinct()
                .ToArray();
            var ambiguityCount = exactMatches.Length > 0
                ? exactMatches.Length
                : matchedObjects.Length;
            if (ambiguityCount > 1)
            {
                issues.Add(Error(
                    assetPath,
                    $"Binding target '{targetName}' has {ambiguityCount} valid matches for {targetTypeName}; runtime resolution would be ambiguous.",
                    matches[0].Component));
            }

            try
            {
                ElementMemberResolver.Resolve(expectedType, memberName);
            }
            catch (MissingMemberException error)
            {
                issues.Add(Error(assetPath, error.Message, matches[0].Component));
            }
            catch (AmbiguousMatchException error)
            {
                issues.Add(Error(assetPath, error.Message, matches[0].Component));
            }

        }

        static List<ElementRecord> CollectElements(GameObject root, bool stopAtNestedBoundary)
        {
            var result = new List<ElementRecord>();
            var queue = new Queue<Transform>();
            queue.Enqueue(root.transform);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var components = current.GetComponents<MonoBehaviour>()
                    .Where(component => component is IElement)
                    .ToArray();
                foreach (var component in components)
                {
                    result.Add(new ElementRecord
                    {
                        Component = component,
                        Name = current.gameObject.name,
                    });
                }

                // 与运行时 View 扫描规则保持一致：根节点上的容器同样拥有独立作用域。
                var isBoundary = components.Any(component => component is IContainerElement);
                if (stopAtNestedBoundary && isBoundary)
                {
                    continue;
                }

                for (var index = 0; index < current.childCount; index++)
                {
                    queue.Enqueue(current.GetChild(index));
                }
            }

            var rootView = root.GetComponent<View>();
            if (rootView != null)
            {
                result.Add(new ElementRecord { Component = rootView, Name = Element.RootName });
            }
            return result;
        }

        static Type ResolveType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        static IReadOnlyList<Route> DiscoverRoutes()
        {
            var routes = new List<Route>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = Array.FindAll(exception.Types, type => type != null);
                }

                foreach (var type in types.Where(type =>
                    type.IsClass
                    && type.IsAbstract
                    && type.IsSealed
                    && type.IsPublic
                    && type.Name == "Routes"))
                {
                    var initialize = type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                    initialize?.Invoke(null, null);
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (typeof(Route).IsAssignableFrom(property.PropertyType)
                            && property.GetValue(null) is Route route)
                        {
                            routes.Add(route);
                        }
                    }
                }
            }

            return routes
                .GroupBy(route => (route.AssetKey, route.ViewModelType, route.PresenterType))
                .Select(group => group.First())
                .ToArray();
        }

        static IReadOnlyList<BindingManifest> ToList(BindingManifest manifest)
        {
            return manifest == null
                ? Array.Empty<BindingManifest>()
                : new[] { manifest };
        }

        static IReadOnlyList<ValidationIssue> DistinctIssues(IEnumerable<ValidationIssue> issues)
        {
            return issues
                .GroupBy(issue => (issue.Severity, issue.AssetPath, issue.Message))
                .Select(group => group.First())
                .ToArray();
        }

        static ValidationIssue Warning(string assetPath, string message, UnityEngine.Object context = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return new ValidationIssue(ValidationSeverity.Warning, assetPath, message, asset ?? context);
        }

        static ValidationIssue Error(string assetPath, string message, UnityEngine.Object context = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return new ValidationIssue(ValidationSeverity.Error, assetPath, message, asset ?? context);
        }
    }
}
