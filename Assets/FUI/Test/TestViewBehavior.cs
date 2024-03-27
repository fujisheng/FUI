using FUI;
using FUI.Test;

using System.Threading.Tasks;

namespace Assets.FUI.Test
{
    [DefaultViewConfig(asset = "TestView.prefab", layer = Layer.Common, viewType = ViewType.Normal)]
    internal class TestViewBehavior : ViewBehavior<TestViewModel>
    {
        protected override async void OnCreate()
        {
            await Task.Delay(1000);
            VM.Name = new Name { firstName = "test", lastName = "OnCreate" };
            VM.ID = 1;
            VM.Age = 11;
        }

        protected override async void OnOpen(object param)
        {
            await Task.Delay(2000);
            VM.Name = new Name { firstName = "test", lastName = "OnOpen" };
            VM.ID = 2;
            VM.Age = 22;

            UnityEngine.Debug.Log($"OnOpen TestView。。。。。。。。。。。。");
        }
    }
}
