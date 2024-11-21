using Feature;

using FUI.UGUI;

using UnityEngine;
using FUI.Manager;

namespace FUI.Test
{
    public class TestLauncher : MonoBehaviour
    {
        public UIManager UIManager { get; private set; }

        static TestLauncher instance;
        public static TestLauncher Instance
        {
            get
            {
                if(instance == null)
                {
                    if(GameObject.Find("[Test]") == null)
                    {
                        GameObject go = new GameObject("[Test]");
                        instance = go.AddComponent<TestLauncher>();
                    }
                    else
                    {
                        instance = GameObject.Find("[Test]").GetComponent<TestLauncher>();
                    }
                }
                return instance;
            }
        }
        void Awake()
        {
            Models.Instance.Initialize();
            var assetLoaderFactory = new TestAssetLoaderFactory();
            var viewFactory = new UGUIViewFactory(assetLoaderFactory);
            UIManager = new UIManager(viewFactory);
            UIManager.Initialize();
            UnityEngine.Debug.Log($"uiManager  initialize.......");
        }

        void Update()
        {
            UIManager.OnUpdate();
        }
    }
}
