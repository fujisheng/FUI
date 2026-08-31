using FUI.Binding;

using UnityEngine;

using System;
using System.Collections.Generic;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 适用于UGUI的view基类
    /// </summary>
    [DisallowMultipleComponent]
    public partial class View : MonoBehaviour, IView, IExternalValueSynchronizationHost
    {
        /// <summary>
        /// 资源加载器
        /// </summary>
        IAssetLoader assetLoader;
        IElement excludedElement;
        // 一个 View 汇总全部无法事件通知的外部值源，避免每个 Element 单独订阅 Canvas 回调。
        readonly List<IExternalValueSource> externalValueSources = new List<IExternalValueSource>();
        bool externalSyncSubscribed;
        bool initialized;

        public bool ExternalValueSynchronizationEnabled { get; set; }

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
            UpdateSortingOrder();
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

            UpdateSortingOrder();
        }

        /// <summary>
        /// 更新顺序
        /// </summary>
        void UpdateSortingOrder()
        {
            if (this == null || this.gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                // Layer 是层基准，Order 是层内顺序；独立 Canvas 不依赖 Transform 兄弟顺序。
                canvas.overrideSorting = true;
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
            if (initialized)
            {
                return;
            }

            Layer = new BindableProperty<int>(int.MinValue, SetLayer);
            Visible = new BindableProperty<bool>(IsVisible(), SetVisible);
            Order = new BindableProperty<int>(int.MinValue, SetOrder);
            Interactable = new BindableProperty<bool>(true, SetInteractable);
            try
            {
                InitializeElements();
                initialized = true;
            }
            catch
            {
                Release();
                throw;
            }
        }

        /// <summary>
        /// 销毁View
        /// </summary>
        void IView.Destroy()
        {
            if (!initialized || this == null || gameObject == null)
            {
                return;
            }

            var currentAssetLoader = assetLoader;
            Release();
            currentAssetLoader?.DestroyGameObject(gameObject);
            currentAssetLoader?.Release();
        }

        /// <summary>
        /// 释放视图
        /// </summary>
        internal virtual void Release()
        {
            ClearExternalValueSources();
            RemoveAllElements();

            Visible?.Dispose();
            Layer?.Dispose();
            Order?.Dispose();
            Interactable?.Dispose();

            assetLoader = null;
            excludedElement = null;
            initialized = false;
        }

        public void SetExternalValueSources(IReadOnlyList<IExternalValueSource> sources)
        {
            if (sources == null)
            {
                return;
            }

            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                if (source != null && !externalValueSources.Contains(source))
                {
                    externalValueSources.Add(source);
                }
            }
            // 没有外部值源的页面不注册空闲 Canvas 回调。
            if (externalValueSources.Count > 0 && !externalSyncSubscribed)
            {
                Canvas.willRenderCanvases += SynchronizeExternalValues;
                externalSyncSubscribed = true;
            }
        }

        public void ClearExternalValueSources()
        {
            if (externalSyncSubscribed)
            {
                Canvas.willRenderCanvases -= SynchronizeExternalValues;
                externalSyncSubscribed = false;
            }
            externalValueSources.Clear();
        }

        void SynchronizeExternalValues()
        {
            // Covered、Cached、Unbound 页面通过该开关停止轮询；inactive 是额外保护。
            if (!ExternalValueSynchronizationEnabled || this == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            for (var index = 0; index < externalValueSources.Count; index++)
            {
                externalValueSources[index].SynchronizeExternalValues();
            }
        }
    }
}
