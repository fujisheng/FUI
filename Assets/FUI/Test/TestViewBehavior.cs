using FUI;
using FUI.Test;

namespace Assets.FUI.Test
{
    [DefaultViewConfig(asset = "TestView.prefab", layer = Layer.Common, viewType = ViewType.Normal)]
    internal class TestViewBehavior : ViewBehavior<TestViewModel>
    {
        protected override void OnCreate()
        {
            VM.Name = new Name { firstName = "test", lastName = "1Name" };
            VM.ID = 1;
            VM.Age = 10;
        }

        public override void OnOpen(object param)
        {
            VM.Name = new Name { firstName = "test", lastName = "openName" };
            VM.ID = 2;
            VM.Age = 11;

            UnityEngine.Debug.Log($"OnOpen TestView。。。。。。。。。。。。");
        }
    }
}
