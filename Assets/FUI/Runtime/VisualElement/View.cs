using FUI.Bindable;

using System;
using System.Collections.Generic;

namespace FUI
{
    public abstract partial class View
    {
        #region 视觉元素查找键
        readonly struct ElementKey : IEquatable<ElementKey>
        {
            public readonly string elementPath;
            public readonly string elementType;

            public ElementKey(string elementPath, string elementType)
            {
                this.elementPath = elementPath;
                this.elementType = elementType;
            }

            public ElementKey(string elementPath, Type elementType)
            {
                this.elementPath = elementPath;
                this.elementType = elementType.FullName;
            }

            public bool IsEmpty => elementPath == null && elementType == null;

            public bool Equals(ElementKey other)
            {
                return elementPath == other.elementPath && elementType == other.elementType;
            }

            public override int GetHashCode()
            {
                return elementPath.GetHashCode() ^ elementType.GetHashCode();
            }

            public override string ToString()
            {
                return $"name:{elementPath} type:{elementType}";
            }
        }
        #endregion

        /// <summary>
        /// 当前绑定的上下文
        /// </summary>
        protected ObservableObject BindingContext { get; private set; }

        /// <summary>
        /// 界面名
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 元素查找表
        /// </summary>
        Dictionary<ElementKey, IVisualElement> elementMap;

        /// <summary>
        /// 初始化这个View
        /// </summary>
        public View(string name)
        {
            this.Name = name;
            elementMap = new Dictionary<ElementKey, IVisualElement>();
        }

        /// <summary>
        /// 通过一个上下文初始化这个View
        /// </summary>
        /// <param name="bindingContext"></param>
        public View(ObservableObject bindingContext, string name) : this(name)
        {
            Binding(bindingContext);
        }

        /// <summary>
        /// 当绑定的上下文属性更改的时候
        /// </summary>
        /// <param name="sender">绑定的上下文</param>
        /// <param name="propertyName">更改的属性名</param>
        protected virtual void OnPropertyChanged(object sender, string propertyName) { }

        /// <summary>
        /// 绑定一个上下文
        /// </summary>
        /// <param name="bindingContext"></param>
        /// <exception cref="System.Exception"></exception>
        public virtual void Binding(ObservableObject bindingContext)
        {
            if(this.BindingContext != null)
            {
                throw new System.Exception($"{Name} binding error: bindingContext not null, you must unbinding first");
            }

            this.BindingContext = bindingContext ?? throw new System.Exception($"{Name} binding error: bindingContext is null");
            this.BindingContext.PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// 解绑上下文
        /// </summary>
        public virtual void Unbinding()
        {
            if(this.BindingContext == null)
            {
                return;
            }

            this.BindingContext.PropertyChanged -= OnPropertyChanged;
            this.BindingContext = null;
        }

        /// <summary>
        /// 添加一个视觉元素
        /// </summary>
        /// <param name="elementName">这个视觉元素的名字</param>
        /// <param name="visualElement">要添加的视觉元素</param>
        protected void AddVisualElement(string elementName, IVisualElement visualElement)
        {
            var key = new ElementKey(elementName, visualElement.GetType());
            if(elementMap.ContainsKey(key))
            {
                UnityEngine.Debug.LogWarning($"{Name} already contains element {key} will replace it");
            }
            elementMap[key] = visualElement;
        }

        /// <summary>
        /// 移除一个视觉元素
        /// </summary>
        /// <param name="visualElement">要移除的视觉元素</param>
        protected void RemoveVisualElement(IVisualElement visualElement)
        {
            var removeKey = default(ElementKey);
            foreach(var kv in elementMap)
            {
                if(kv.Value == visualElement)
                {
                    removeKey = kv.Key;
                    break;
                }
            }
            if(!removeKey.IsEmpty)
            {
                elementMap.Remove(removeKey);
            }
        }

        /// <summary>
        /// 移除所有的视觉元素
        /// </summary>
        protected void RemoveVisualElements()
        {
            elementMap.Clear();
        }

        /// <summary>
        /// 获取一个视觉元素
        /// </summary>
        /// <typeparam name="TVisualElement">元素类型</typeparam>
        /// <param name="elementName">元素名</param>
        /// <returns></returns>
        protected TVisualElement GetVisualElement<TVisualElement>(string elementName) where TVisualElement : IVisualElement
        {
            var key = new ElementKey(elementName, typeof(TVisualElement));
            if(!elementMap.TryGetValue(key, out var visualElement))
            {
                return default;
            }
            return (TVisualElement)visualElement;
        }

        /// <summary>
        /// 当这个界面被销毁的时候
        /// </summary>
        public virtual void OnDestroy()
        {
            Unbinding();
            RemoveVisualElements();
        }
    }
}
