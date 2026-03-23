using FUI.Bindable;

using UnityEngine;

using System;

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
        IAssetLoader assetLoader;

        #region IView
        string viewName;

        /// <summary>
        /// 界面名字
        /// </summary>
        string IView.Name
        {
            get 
            {
                if (string.IsNullOrEmpty(viewName))
                {
                    viewName = Utility.RemoveCloneSuffix(gameObject.name);
                }
                return viewName;
            }
        }

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
        /// 可见性
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
        /// 可交互性
        /// </summary>
        bool IView.Interactable
        {
            get { return Interactable.Value; }
            set { Interactable.Value = value; }
        }

        /// <summary>
        /// 是否可见 
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsVisible()
        {
            return gameObject.activeSelf;
        }
        #endregion

        /// <summary>
        /// 设置可见性
        /// </summary>
        /// <param name="oldVisible">之前的值</param>
        /// <param name="visible">现在的值</param>
        protected virtual void SetVisible(bool oldVisible, bool visible)
        {
            if (this == null || this.gameObject == null)
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

        /// <summary>
        /// 设置资源加载器
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        protected virtual void SetAssetLoader(IAssetLoader assetLoader)
        {
            this.assetLoader = assetLoader;
        }

        /// <summary>
        /// 初始化View
        /// </summary>
        void IView.Initialize()
        {
            Layer = new BindableProperty<int>(int.MinValue, SetLayer);
            Visible = new BindableProperty<bool>(IsVisible(), SetVisible);
            Order = new BindableProperty<int>(int.MinValue, SetOrder);
            Interactable = new BindableProperty<bool>(true, SetInteractable);

            InitializeElements();
        }

        /// <summary>
        /// 销毁View
        /// </summary>
        void IView.Destroy()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            assetLoader?.DestroyGameObject(gameObject);
            assetLoader?.Release();
            Release();
        }

        /// <summary>
        /// 释放视图
        /// </summary>
        internal virtual void Release()
        {
            RemoveAllElements();

            Visible?.Dispose();
            Layer?.Dispose();
            Order?.Dispose();
            Interactable?.Dispose();

            assetLoader = null;
        }
    }
}
