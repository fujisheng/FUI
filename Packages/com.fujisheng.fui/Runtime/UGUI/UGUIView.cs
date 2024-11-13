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

        /// <summary>
        /// 层级属性
        /// </summary>
        public BindableProperty<int> LayerProperty { get; private set; }

        /// <summary>
        /// 层级
        /// </summary>
        int IView.Layer 
        { 
            get { return LayerProperty.Value; }
            set { LayerProperty.Value = value; } 
        }

        /// <summary>
        /// 顺序属性
        /// </summary>
        public BindableProperty<int> OrderProperty { get; private set; }

        /// <summary>
        /// 顺序
        /// </summary>
        int IView.Order
        {
            get { return OrderProperty.Value; }
            set { OrderProperty.Value = value; }
        }

        /// <summary>
        /// 可见性属性
        /// </summary>
        public BindableProperty<bool> VisibleProperty { get; private set; }

        /// <summary>
        /// 可见性
        /// </summary>
        bool IView.Visible
        {
            get { return VisibleProperty.Value; }
            set { VisibleProperty.Value = value; }
        }

        /// <summary>
        /// 可交互属性
        /// </summary>
        public BindableProperty<bool> Interactable { get; private set; }

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

        /// <summary>
        /// 内部初始化
        /// </summary>
        void InternalInitialize()
        {
            LayerProperty = new BindableProperty<int>(GetLayer());
            VisibleProperty = new BindableProperty<bool>(IsVisible());
            OrderProperty = new BindableProperty<int>(GetOrder());
            Interactable = new BindableProperty<bool>(true);

            LayerProperty.OnValueChanged += SetLayer;
            VisibleProperty.OnValueChanged += SetVisible;
            OrderProperty.OnValueChanged += SetOrder;
            Interactable.OnValueChanged += OnSetInteractable;

            Initialize();
        }

        /// <summary>
        /// 是否可见 
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsVisible()
        {
            return gameObject.activeSelf;
        }

        /// <summary>
        /// 获取层级
        /// </summary>
        /// <returns></returns>
        protected virtual int GetLayer()
        {
            if(!gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                return 0;
            }
            return canvas.sortingOrder;
        }

        /// <summary>
        /// 获取顺序
        /// </summary>
        /// <returns></returns>
        protected virtual int GetOrder()
        {
            return gameObject.transform.GetSiblingIndex();
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        /// <param name="oldVisible">之前的值</param>
        /// <param name="visible">现在的值</param>
        protected virtual void SetVisible(bool oldVisible, bool visible)
        {
            this.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 设置层级
        /// </summary>
        /// <param name="oldLayer">之前的值</param>
        /// <param name="layer">现在的值</param>
        protected virtual void SetLayer(int oldLayer, int layer)
        {
            if (!gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                return;
            }
            canvas.sortingOrder = layer;
        }

        /// <summary>
        /// 设置顺序
        /// </summary>
        /// <param name="oldOrder">之前的值</param>
        /// <param name="order">现在的值</param>
        protected virtual void SetOrder(int oldOrder, int order)
        {
            gameObject.transform.SetSiblingIndex(order);
        }

        /// <summary>
        /// 设置可交互属性
        /// </summary>
        /// <param name="oldInteractable">之前的值</param>
        /// <param name="interactable">现在的值</param>
        protected virtual void OnSetInteractable(bool oldInteractable, bool interactable)
        {
            if (gameObject.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.blocksRaycasts = interactable;
            }
        }

        /// <summary>
        /// 销毁这个View
        /// </summary>
        void IView.Destroy()
        {
            VisibleProperty.Dispose();
            LayerProperty.Dispose();
            OrderProperty.Dispose();
            Interactable.Dispose();

            AssetLoader.DestroyGameObject(gameObject);
            AssetLoader.Release();

            Destroy();
        }

        /// <summary>
        /// 初始化View
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// 销毁View
        /// </summary>
        protected virtual void Destroy() { }
    }
}
