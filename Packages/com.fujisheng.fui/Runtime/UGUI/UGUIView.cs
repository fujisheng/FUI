using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// 适用于UGUI的view基类
    /// </summary>
    public partial class UGUIView : MonoBehaviour, IView
    {
        /// <summary>
        /// 资源加载器
        /// </summary>
        protected IAssetLoader AssetLoader { get; private set; }

        string viewName;

        /// <summary>
        /// 界面名字
        /// </summary>
        string IView.Name => viewName;

        public BindableProperty<int> LayerProperty { get; private set; }
        /// <summary>
        /// 层级
        /// </summary>
        public int Layer 
        { 
            get { return LayerProperty.Value; }
            set { LayerProperty.Value = value; } 
        }

        public BindableProperty<int> OrderProperty { get; private set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order
        {
            get { return OrderProperty.Value; }
            set { OrderProperty.Value = value; }
        }

        public BindableProperty<bool> VisibleProperty { get; private set; }
        /// <summary>
        /// 可见性
        /// </summary>
        public bool Visible
        {
            get { return VisibleProperty.Value; }
            set { VisibleProperty.Value = value; }
        }

        /// <summary>
        /// 创建一个UGUIView
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="assetPath">资源路径</param>
        /// <param name="viewName">view名字</param>
        /// <returns></returns>
        public static UGUIView Create(IAssetLoader assetLoader, string assetPath, string viewName)
        {
            var go = assetLoader.CreateGameObject(assetPath);
            return Create(assetLoader, go, viewName);
        }

        /// <summary>
        /// 创建一个UGUIView
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="go">游戏实体</param>
        /// <param name="viewName">view名字</param>
        /// <returns></returns>
        public static UGUIView Create(IAssetLoader assetLoader, GameObject go, string viewName)
        {
            if (go == null)
            {
                return null;
            }

            if (!go.TryGetComponent<UGUIView>(out var view))
            {
                view = go.AddComponent<UGUIView>();
            }

            view.viewName = viewName;
            view.AssetLoader = assetLoader;
            view.InitializeElements();
            return view;
        }

        public virtual void Initialize()
        {
            LayerProperty = new BindableProperty<int>(GetLayer());
            VisibleProperty = new BindableProperty<bool>(IsVisible());
            OrderProperty = new BindableProperty<int>(GetOrder());

            LayerProperty.PropertySet += SetLayer;
            VisibleProperty.PropertySet += SetVisible;
            OrderProperty.PropertySet += SetOrder;
        }

        protected virtual bool IsVisible()
        {
            return gameObject.activeSelf;
        }

        protected virtual int GetLayer()
        {
            if(!gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                return 0;
            }
            return canvas.sortingOrder;
        }

        protected virtual int GetOrder()
        {
            return gameObject.transform.GetSiblingIndex();
        }

        protected virtual void SetVisible(bool oldVisible, bool visible)
        {
            this.gameObject.SetActive(visible);
        }

        protected virtual void SetLayer(int oldLayer, int layer)
        {
            if (!gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                return;
            }
            canvas.sortingOrder = layer;
        }

        protected virtual void SetOrder(int oldOrder, int order)
        {
            gameObject.transform.SetSiblingIndex(order);
        }

        public virtual void Destroy()
        {
            VisibleProperty.ClearEvent();
            LayerProperty.ClearEvent();
            OrderProperty.ClearEvent();

            AssetLoader.DestroyGameObject(gameObject);
            AssetLoader.Release();
        }
    }
}
