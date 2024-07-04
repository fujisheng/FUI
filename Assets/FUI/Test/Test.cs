using Feature;

using FUI.UGUI;

using UnityEngine;
using FUI.Manager;

namespace FUI.Test
{
    public class Test : MonoBehaviour
    {
        public UIManager UIManager { get; private set; }

        static Test instance;
        public static Test Instance
        {
            get
            {
                if(instance == null)
                {
                    if(GameObject.Find("[Test]") == null)
                    {
                        GameObject go = new GameObject("[Test]");
                        instance = go.AddComponent<Test>();
                    }
                    else
                    {
                        instance = GameObject.Find("[Test]").GetComponent<Test>();
                    }
                }
                return instance;
            }
        }
        void Awake()
        {
            Models.Instance.Initialize();
            UIManager = new UIManager(new UGUIViewFactory(()=>new TestAssetLoader()));
            UIManager.Initialize();
            UnityEngine.Debug.Log($"uiManager  initialize.......");
        }

        void Update()
        {
            UIManager.OnUpdate();
        }
    }
}
