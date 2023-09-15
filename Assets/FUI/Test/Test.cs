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
            uiManager = new UIManager(new UGUIViewCreator(()=>new TestAssetLoader()));
            uiManager.Initialize();
            UnityEngine.Debug.Log($"uiManager  initialize........");
        }

        void Start()
        {
            uiManager.Open(Views.TestView);
        }
    }
}
