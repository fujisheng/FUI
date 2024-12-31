using FUI;
using FUI.Test;
using FUI.UGUI;
using FUI.UGUI.Control;

using PlasticGui.WorkspaceWindow.Items;

using System.Threading.Tasks;

using UnityEngine;

namespace Test.Entity
{
    [Binding("TestUIEntityView")]
    public class TestUIEntityViewModel : ViewModel
    {
        [Binding("txt_string", nameof(TextElement.Text), bindingMode:BindingMode.OneWay)]
        public string TestString { get; set; }
    }

    public class TestUIEntityViewModel2 : TestUIEntityViewModel 
    {
        [Binding("txt_string", nameof(TextElement.Text), bindingMode: BindingMode.OneWay)]
        public string TestString2 { get; set; } = "TestString2";
    }

    public class TestUIEntityViewBehavior : ViewBehavior<TestUIEntityViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.TestString = $"Hello, ViewModel1:{this.VM}";
        }

        protected override void OnUpdateViewModel(TestUIEntityViewModel oldViewModel, TestUIEntityViewModel newViewModel)
        {
            VM.TestString = $"Hello, ViewModel2:{newViewModel}";
        }
    }

    public class TestUIEntity : MonoBehaviour
    {
        async void Start()
        {
            var assetLoaderFactory = new TestAssetLoaderFactory();
            var viewFactory = new UGUIViewFactory(assetLoaderFactory);

            var entity = UIEntity.Create("TestUIEntityView", viewFactory);
            UnityEngine.Debug.Log("TestUIEntity created.");
            entity.Enable();

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity UpdateViewModel");
            entity.UpdateViewModel(new TestUIEntityViewModel2());

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity Disable");
            entity.Disable();

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity Enable");
            entity.Enable();

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity Freeze");
            entity.Freeze();

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity Unfreeze");
            entity.Unfreeze(viewFactory);

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity Destroy");
            entity.Destroy();

            await Task.Delay(2000);
            Debug.Log($"TestUIEntity Enable");
            entity.Enable();
        }
    }
}
