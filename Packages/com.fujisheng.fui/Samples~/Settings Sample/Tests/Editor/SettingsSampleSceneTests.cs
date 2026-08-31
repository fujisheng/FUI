using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FUI.Samples.Settings.Tests
{
    public sealed class SettingsSampleSceneTests
    {
        [Test]
        public void ImportedSceneContainsBootstrap()
        {
            var sceneGuids = AssetDatabase.FindAssets("SettingsSample t:Scene");
            Assert.That(sceneGuids, Has.Length.EqualTo(1));

            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(Object.FindAnyObjectByType<SettingsSampleBootstrap>(), Is.Not.Null);
        }
    }
}
