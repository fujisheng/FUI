using FUI.UGUI;

using UnityEngine;

namespace FUI.Test
{
    public class Views
    {
        public const string TestView = "TestView";
    }

    [Feature("Test")]
    [AutoConstructOnInitialization]
    public class TestData : IData
    {
        public void Initialize()
        {
            UnityEngine.Debug.Log($"TestData Initialization");
        }
    }

    public class Test : MonoBehaviour
    {
        UIManager uiManager;
        DataManager dataManager;
        void Awake()
        {
            dataManager = new DataManager();
            dataManager.Initialize();
            uiManager = new UIManager(new UGUIBuilder(()=>new TestAssetLoader()));
            uiManager.Initialize();
            UnityEngine.Debug.Log($"uiManager  initialize.......");
        }

        void Update()
        {
            uiManager.OnUpdate();
        }

        async void Start()
        {
           await  uiManager.OpenAsync(Views.TestView);
            UnityEngine.Debug.Log($"open async testView");
        }
    }
}
