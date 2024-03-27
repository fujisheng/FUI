using FUI.UGUI;

using UnityEngine;
using Feature;

namespace FUI.Test
{
    public class Views
    {
        public const string TestView = "TestView";
    }

    [AutoConstructOnInitialization]
    public class TestModel : IModel
    {
        public int id;
        public string name;
        public int age;
        public void Initialize()
        {
            UnityEngine.Debug.Log($"TestModel Init");
        }

        public void Release()
        {
            UnityEngine.Debug.Log($"TestModel Release");
        }
    }

    public class Test : MonoBehaviour
    {
        UIManager uiManager;
        void Awake()
        {
            Models.Instance.Initialize();
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
