using FUI.UGUI;

using UnityEngine;

namespace FUI.Test
{
    public class Views
    {
        public const string TestView = "TestView";
    }

    public class Test : MonoBehaviour
    {
        UIManager uiManager;
        void Awake()
        {
            uiManager = new UIManager(new UGUIViewBuilder(()=>new TestAssetLoader()));
            uiManager.Initialize();
            UnityEngine.Debug.Log($"uiManager  initialize........");
        }

        void Update()
        {
            uiManager.OnUpdate();
        }

        void Start()
        {
            uiManager.Open(Views.TestView);
        }
    }
}
