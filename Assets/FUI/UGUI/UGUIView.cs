using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// 适用于UGUI的view基类
    /// </summary>
    public class UGUIView : View
    {
        protected GameObject gameObject;
        protected IAssetLoader assetLoader;

        /// <summary>
        /// 初始化一个UGUIView
        /// </summary>
        /// <param name="bindingContext">绑定的上下文</param>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="assetPath">这个view的资源路径</param>
        public UGUIView(ObservableObject bindingContext, IAssetLoader assetLoader, string assetPath, string viewName) : base(bindingContext, viewName)
        {
            this.assetLoader = assetLoader;
            this.gameObject = assetLoader.CreateGameObject(assetPath);
            InitializeVisualElements();
        }

        /// <summary>
        /// 初始化一个UGUIView
        /// </summary>
        /// <param name="gameObject">这个view对应的gameobject</param>
        /// <param name="bindingContext">绑定的上下文</param>
        /// <param name="assetLoader">这个view对应的资源加载器</param>
        public UGUIView(ObservableObject bindingContext, IAssetLoader assetLoader, GameObject gameObject, string viewName) : base(bindingContext, viewName)
        {
            this.assetLoader = assetLoader;
            this.gameObject = gameObject;
            InitializeVisualElements();
        }

        /// <summary>
        /// 初始化这个界面的视觉元素
        /// </summary>
        protected virtual void InitializeVisualElements()
        {
            //获取所有的视觉元素组件
            foreach (var element in gameObject.transform.GetComponentsInChildren<UGUIVisualElement>(true))
            {
                element.SetAssetLoader(assetLoader);
                AddVisualElement(element.name, element);
            }
        }

        /// <summary>
        /// 暂时直接SetActive
        /// </summary>
        protected override void Enable()
        {
            base.Enable();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 暂时直接SetActive
        /// </summary>
        protected override void Disable()
        {
            base.Disable();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 当这个界面被销毁的时候
        /// </summary>
        protected override void Destroy()
        {
            base.Destroy();
            assetLoader.DestroyGameObject(gameObject);
            assetLoader.Release();
        }
    }
}
