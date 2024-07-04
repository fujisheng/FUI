﻿using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// 适用于UGUI的view基类
    /// </summary>
    public class UGUIView : IView
    {
        /// <summary>
        /// 视图对应的GameObject
        /// </summary>
        protected GameObject gameObject;

        /// <summary>
        /// 资源加载器
        /// </summary>
        protected IAssetLoader assetLoader;

        /// <summary>
        /// 层级
        /// </summary>
        int layer;

        /// <summary>
        /// 顺序
        /// </summary>
        int order;

        /// <summary>
        /// 存储所有的视觉元素
        /// </summary>
        Dictionary<ElementKey, IVisualElement> elements;

        /// <summary>
        /// 存储默认的视觉元素
        /// </summary>
        Dictionary<string, IVisualElement> defaultElements;

        /// <summary>
        /// 界面名字
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 层级
        /// </summary>
        public int Layer
        {
            get
            {
                return layer;
            }
            set
            {
                layer = value;
                SetLayer(layer);
            }
        }

        /// <summary>
        /// 顺序
        /// </summary>
        public int Order
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
                SetOrder(order);
            }
        }

        /// <summary>
        /// 构造一个UGUI视图
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="assetPath">视图对应资源路径</param>
        /// <param name="viewName">视图名字</param>
        public UGUIView(IAssetLoader assetLoader, string assetPath, string viewName)
        {
            this.Name = viewName;
            this.assetLoader = assetLoader;
            this.gameObject = assetLoader.CreateGameObject(assetPath);
            InitializeVisualElements();
        }

        /// <summary>
        /// 初始化一个UGUI视图
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="gameObject">视图对应的GameObject</param>
        /// <param name="viewName">视图名字</param>
        public UGUIView(IAssetLoader assetLoader, GameObject gameObject, string viewName)
        {
            this.Name = viewName;
            this.assetLoader = assetLoader;
            this.gameObject = gameObject;
            InitializeVisualElements();
        }

        /// <summary>
        /// 初始化这个界面的视觉元素
        /// </summary>
        protected virtual void InitializeVisualElements()
        {
            if(elements == null)
            {
                elements = new Dictionary<ElementKey, IVisualElement>();
                defaultElements = new Dictionary<string, IVisualElement>();
            }

            //获取所有的视觉元素组件
            foreach (var element in gameObject.transform.GetComponentsInChildren<UGUIVisualElement>(true))
            {
                element.SetAssetLoader(assetLoader);
                AddVisualElement(element.name, element);
                element.InternalInitialize();
            }
        }

        /// <summary>
        /// 添加一个视觉元素
        /// </summary>
        /// <param name="elementName">这个视觉元素的名字</param>
        /// <param name="visualElement">要添加的视觉元素</param>
        protected void AddVisualElement(string elementName, IVisualElement visualElement)
        {
            var key = new ElementKey(elementName, visualElement.GetType());
            if (elements.ContainsKey(key))
            {
                UnityEngine.Debug.LogWarning($"{Name} already contains element {key} will replace it");
            }
            elements[key] = visualElement;

            if (!defaultElements.ContainsKey(elementName))
            {
                defaultElements[elementName] = visualElement;
            }
        }

        /// <summary>
        /// 获取一个视觉元素
        /// </summary>
        /// <typeparam name="TVisualElement">视觉元素类型</typeparam>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public TVisualElement GetVisualElement<TVisualElement>(string path) where TVisualElement : IVisualElement
        {
            var key = new ElementKey(path, typeof(TVisualElement));
            if (!elements.TryGetValue(key, out var visualElement))
            {
                return default;
            }
            return (TVisualElement)visualElement;
        }

        /// <summary>
        /// 获取一个视觉元素 如果有多个视觉元素则返回第一个
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public IVisualElement GetVisualElement(string path)
        {
            if (!defaultElements.TryGetValue(path, out var visualElement))
            {
                return default;
            }
            return visualElement;
        }

        void IView.Enable()
        {
            gameObject.SetActive(true);
        }

        void IView.Disable()
        {
            gameObject.SetActive(false);
        }

        void IView.Destroy()
        {
            assetLoader.DestroyGameObject(gameObject);
            assetLoader.Release();
        }

        protected virtual void SetLayer(int layer)
        {
            
        }

        protected virtual void SetOrder(int order)
        {
            gameObject.transform.SetSiblingIndex(order);
        }
    }
}