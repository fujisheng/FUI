using System;

using UnityEngine;

namespace FUI.Test
{
    public class Test : MonoBehaviour
    {
        void Awake()
        {
            var vm1 = OpenView<TestViewModel>("TestView");
            vm1.Name = new Name { firstName = "test", lastName = "1Name" };
            vm1.ID = 1;
            vm1.Age = 10;
        }

        TViewModel OpenView<TViewModel>(string viewName) where TViewModel : ViewModel
        {
            var vm = Activator.CreateInstance<TViewModel>();
            var assetLoader = new TestAssetLoader();
            var assetPath = viewName;
            //TODO 这个地方存在字符串拼接   这个可以通过注入解决
            var viewTypeName = $"__DataBindingGenerated.__{viewName}_Binding_Generated";
            var viewType = Type.GetType(viewTypeName);
            var viewObj = assetLoader.CreateGameObject(assetPath);
            var view = Activator.CreateInstance(viewType, vm, assetLoader, viewObj, viewName) as View;
            UnityEngine.Debug.Log($"OpenViewComplete   :{view.Name}");
            //vm.Initialize();
            return vm;
        }
    }
}
