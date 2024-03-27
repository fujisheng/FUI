using FUI.UGUI;

using UnityEngine;
using Feature;
using System.Threading.Tasks;

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

            Models.Instance.GetGroup<PublicGroup>().AddModifyListener<TestModel>(OnTestModelChanged);
        }

        void OnTestModelChanged(TestModel t)
        {
            UnityEngine.Debug.Log($"id:{t.id} name:{t.name} age:{t.age}");
        }

        void Update()
        {
            uiManager.OnUpdate();
        }

        async void Start()
        {
           await  uiManager.OpenAsync(Views.TestView);
            UnityEngine.Debug.Log($"open async testView");

            using (Models.Instance.GetGroup<PublicGroup>().Get<TestModel>(out var testModel))
            {
                testModel.id = 2;
                testModel.name = "sss";
                testModel.age = 40;
            }
            await Task.Delay(1000);
            using (Models.Instance.GetGroup<PublicGroup>().Get<TestModel>(out var testModel))
            {
                testModel.id = 3;
                testModel.name = "sss2";
                testModel.age = 402;
            }
            await Task.Delay(1000);
            using (Models.Instance.GetGroup<PublicGroup>().Get<TestModel>(out var testModel))
            {
                testModel.id = 4;
                testModel.name = "sss3";
                testModel.age = 403;
            }
        }
    }
}
