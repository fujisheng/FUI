using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// 适用于UGUI的view基类
    /// </summary>
    public partial class View : MonoBehaviour, IView
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
        public BindableProperty<int> Layer { get; private set; }

        /// <summary>
        /// 层级
        /// </summary>
        int IView.Layer 
        { 
            get { return Layer.Value; }
            set { Layer.Value = value; } 
        }

        /// <summary>
        /// 顺序属性
        /// </summary>
        public BindableProperty<int> Order { get; private set; }

        /// <summary>
        /// 顺序
        /// </summary>
        int IView.Order
        {
            get { return Order.Value; }
            set { Order.Value = value; }
        }

        /// <summary>
        /// 可见性属性
        /// </summary>
        public BindableProperty<bool> Visible { get; private set; }

        /// <summary>
        /// 可见性
        /// </summary>
        bool IView.Visible
        {
            get { return Visible.Value; }
            set { Visible.Value = value; }
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
        /// <param name="initialized">已初始化的</param>
        /// <returns></returns>
        public static View Create(IAssetLoader assetLoader, string assetPath, string viewName, bool initialized = false)
        {
            var go = assetLoader.CreateGameObject(assetPath);
            return Create(assetLoader, go, viewName, initialized);
        }

        /// <summary>
        /// 创建一个UGUIView
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="go">游戏实体</param>
        /// <param name="viewName">view名字</param>
        /// <param name="initialized">已初始化的</param>
        /// <returns></returns>
        public static View Create(IAssetLoader assetLoader, GameObject go, string viewName, bool initialized = false)
        {
            if (go == null)
            {
                return null;
            }

            if (!go.TryGetComponent<View>(out var view))
            {
                view = go.AddComponent<View>();
            }

            view.viewName = viewName;
            view.AssetLoader = assetLoader;

            //如果没有初始化，需要初始化
            if (!initialized)
            {
                view.InternalInitialize();
            }
            return view;
        }

        /// <summary>
        /// 内部初始化
        /// </summary>
        void InternalInitialize()
        {
            Layer = new BindableProperty<int>(GetLayer(), SetLayer);
            Visible = new BindableProperty<bool>(IsVisible(), SetVisible);
            Order = new BindableProperty<int>(GetOrder(), SetOrder);
            Interactable = new BindableProperty<bool>(true, SetInteractable);

            //初始化所有的子元素
            InitializeElements();

            //初始化View
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
        protected virtual void SetInteractable(bool oldInteractable, bool interactable)
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
            Visible.Dispose();
            Layer.Dispose();
            Order.Dispose();
            Interactable.Dispose();

            DestroyChildren();

            AssetLoader.DestroyGameObject(gameObject);
            AssetLoader.Release();

            Destroy();
        }

        void DestroyChildren()
        {
            foreach (var child in Children)
            {
                (child as View).Destroy();
            }
            elements.Clear();
            namedElements.Clear();
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
