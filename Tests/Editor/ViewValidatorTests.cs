using FUI.Editor.Validation;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Tests
{
    public sealed class ViewValidatorTests
    {
        const string TestAssetFolder = "Assets/__FUIViewValidatorTests__";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "__FUIViewValidatorTests__");
            }
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestAssetFolder);
        }

        [Test]
        public void MissingRootViewProducesValidationError()
        {
            var path = SavePrefab(new GameObject("MissingView", typeof(RectTransform)));

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(ValidationSeverity.Error));
            Assert.That(issues[0].Message, Does.Contain("missing a View component"));
        }

        [Test]
        public void DuplicateElementNamesAndTypesProduceSingleError()
        {
            var root = CreateValidViewRoot("DuplicateElements");
            AddElement(root, "Entry");
            AddElement(root, "Entry");
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues.Count(issue => issue.Severity == ValidationSeverity.Error), Is.EqualTo(1));
            Assert.That(issues.Single().Message, Does.Contain("Duplicate element name and type"));
        }

        [Test]
        public void RootContainerExcludesItsChildProjectionScopeFromDuplicateValidation()
        {
            var root = CreateValidViewRoot("RootContainerView");
            var container = root.AddComponent<SlotListElement>();
            var serialized = new SerializedObject(container);
            serialized.FindProperty("content").objectReferenceValue = root.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddElement(root, "Img_Icon");
            AddElement(root, "Img_Icon");
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void StructurallyValidPrefabWithoutBindingsHasNoIssues()
        {
            var root = CreateValidViewRoot("ValidView");
            AddElement(root, "Title");
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TextBackendConflictRuleRejectsLegacyAndTmpCombination()
        {
            Assert.That(ViewValidator.HasMultipleTextBackends(1, 1), Is.True);
            Assert.That(ViewValidator.HasMultipleTextBackends(2, 0), Is.True);
            Assert.That(ViewValidator.HasMultipleTextBackends(0, 2), Is.True);
            Assert.That(ViewValidator.HasMultipleTextBackends(1, 0), Is.False);
            Assert.That(ViewValidator.HasMultipleTextBackends(0, 1), Is.False);
        }

        [Test]
        public void RecyclingListWithoutTemplateProducesValidationError()
        {
            var root = CreateValidViewRoot("MissingListTemplate");
            AddChild(root, "Items").AddComponent<RecyclingListElement>();
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues.Any(issue => issue.Message.Contains("requires an item prefab")), Is.True);
        }

        [Test]
        public void SlotAndAnchoredListsWithoutContainersProduceValidationErrors()
        {
            var root = CreateValidViewRoot("MissingListContainers");
            AddChild(root, "Slots").AddComponent<SlotListElement>();
            AddChild(root, "Anchors").AddComponent<AnchoredListElement>();
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues.Any(issue => issue.Message.Contains("SlotListElement") && issue.Message.Contains("requires Content")), Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("AnchoredListElement") && issue.Message.Contains("requires at least one anchor")), Is.True);
        }

        [Test]
        public void ScrollViewWithoutContentProducesValidationError()
        {
            var root = CreateValidViewRoot("MissingScrollContent");
            AddChild(root, "Scroll").AddComponent<ScrollViewElement>();
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues.Any(issue => issue.Message.Contains("requires ScrollRect.content")), Is.True);
        }

        [Test]
        public void LegacyTextWithMatchingBackendIsValid()
        {
            var root = CreateValidViewRoot("ValidLegacyText");
            var child = AddChild(root, "Title");
            child.AddComponent<Text>();
            child.AddComponent<LegacyTextElement>();
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void InputFieldAndDropdownWithoutRequiredReferencesProduceErrors()
        {
            var root = CreateValidViewRoot("InvalidControls");
            var input = AddChild(root, "Input");
            input.AddComponent<InputField>();
            input.AddComponent<LegacyInputFieldElement>();
            var dropdown = AddChild(root, "Dropdown");
            dropdown.AddComponent<Dropdown>();
            dropdown.AddComponent<LegacyDropdownElement>();
            var path = SavePrefab(root);

            var issues = ViewValidator.ValidatePrefab(path);

            Assert.That(issues.Any(issue => issue.Message.Contains("requires InputField.textComponent")), Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("requires template, captionText and itemText")), Is.True);
        }

        static GameObject CreateValidViewRoot(string name)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster));
            root.AddComponent<View>();
            return root;
        }

        static void AddElement(GameObject root, string name)
        {
            var child = AddChild(root, name);
            child.AddComponent<RectTransformElement>();
        }

        static GameObject AddChild(GameObject root, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child;
        }

        static string SavePrefab(GameObject root)
        {
            var path = $"{TestAssetFolder}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }
    }
}
