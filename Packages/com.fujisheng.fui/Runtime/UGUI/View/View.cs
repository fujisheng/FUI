using FUI.Bindable;

using System;

using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// 适用于UGUI的view基类
    /// </summary>
    public partial class View : UIElement, IView
    {
        string viewName;

        /// <summary>
        /// 界面名字
        /// </summary>
        string IView.Name => string.IsNullOrEmpty(viewName) ? (this as IElement).Name : viewName;

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
        /// 可交互属性
        /// </summary>
        public BindableProperty<bool> Interactable { get; private set; }

        /// <summary>
        /// 当这个View被创建时
        /// </summary>
        /// <param name="assetLoader">这个view对应的资源加载器</param>
        void OnCreate(IAssetLoader assetLoader) => InternalInitialize(assetLoader);

        /// <summary>
        /// 当这个View被销毁时
        /// </summary>
        void IView.Destroy() => InternalRelease();
        
        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();

            Layer = new BindableProperty<int>(int.MinValue, SetLayer);
            Visible = new BindableProperty<bool>(IsVisible(), SetVisible);
            Order = new BindableProperty<int>(int.MinValue, SetOrder);
            Interactable = new BindableProperty<bool>(true, SetInteractable);

            //初始化所有的子元素
            InitializeElements();
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
        /// 设置可见性
        /// </summary>
        /// <param name="oldVisible">之前的值</param>
        /// <param name="visible">现在的值</param>
        protected virtual void SetVisible(bool oldVisible, bool visible)
        {
            if(this == null || this.gameObject == null)
            {
                return;
            }

            this.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 设置层级
        /// </summary>
        /// <param name="oldLayer">之前的值</param>
        /// <param name="layer">现在的值</param>
        protected virtual void SetLayer(int oldLayer, int layer)
        {
            UpdateShoringOrder();
        }

        /// <summary>
        /// 设置顺序
        /// </summary>
        /// <param name="oldOrder">之前的值</param>
        /// <param name="order">现在的值</param>
        protected virtual void SetOrder(int oldOrder, int order)
        {
            if (this == null || this.gameObject == null)
            {
                return;
            }

            gameObject.transform.SetSiblingIndex(order);
            UpdateShoringOrder();
        }

        /// <summary>
        /// 更新顺序
        /// </summary>
        void UpdateShoringOrder()
        {
            if (this == null || this.gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                canvas.sortingOrder = Layer.Value + Order.Value;
            }
        }

        /// <summary>
        /// 设置可交互属性
        /// </summary>
        /// <param name="oldInteractable">之前的值</param>
        /// <param name="interactable">现在的值</param>
        protected virtual void SetInteractable(bool oldInteractable, bool interactable)
        {
            if (this == null || this.gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.blocksRaycasts = interactable;
            }
        }

        protected override void OnRelease()
        {
            if(this == null || gameObject == null)
            {
                return;
            }

            base.OnRelease();

            ReleaseChildren();

            Visible.Dispose();
            Layer.Dispose();
            Order.Dispose();
            Interactable.Dispose();

            AssetLoader?.DestroyGameObject(gameObject);
            AssetLoader?.Release();
        }

        void ReleaseChildren()
        {
            foreach (var child in Elements)
            {
                if (child == null || child.Equals(this))
                {
                    continue;
                }

                if (child is Element element)
                {
                    try
                    {
                        element.InternalRelease();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogException(ex);
                    }
                }
            }

            ClearElements();
        }
    }
}
