using FUI.Bindable;

using System.Collections.Generic;

namespace FUI
{
    public abstract partial class View : IView
    {
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
        readonly Dictionary<ElementKey, IVisualElement> elementMap;

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
            (this as IView).Binding(bindingContext);
        }

        /// <summary>
        /// 当绑定的上下文属性更改的时候
        /// </summary>
        /// <param name="sender">绑定的上下文</param>
        /// <param name="propertyName">更改的属性名</param>
        protected virtual void OnPropertyChanged(object sender, string propertyName) { }

        #region VisualElement
        /// <summary>
        /// 添加一个视觉元素
        /// </summary>
        /// <param name="elementName">这个视觉元素的名字</param>
        /// <param name="visualElement">要添加的视觉元素</param>
        protected void AddVisualElement(string elementName, IVisualElement visualElement)
        {
            var key = new ElementKey(elementName, visualElement.GetType());
            if (elementMap.ContainsKey(key))
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
            foreach (var kv in elementMap)
            {
                if (kv.Value == visualElement)
                {
                    removeKey = kv.Key;
                    break;
                }
            }
            if (!removeKey.IsEmpty)
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
            if (!elementMap.TryGetValue(key, out var visualElement))
            {
                return default;
            }
            return (TVisualElement)visualElement;
        }
        #endregion

        #region IView
        /// <summary>
        /// 当前绑定的上下文
        /// </summary>
        ObservableObject IView.BindingContext => BindingContext;

        /// <summary>
        /// 绑定一个上下文
        /// </summary>
        /// <param name="bindingContext"></param>
        /// <exception cref="System.Exception"></exception>
        void IView.Binding(ObservableObject bindingContext)
        {
            if(this.BindingContext != null)
            {
                throw new System.Exception($"{Name} binding error: bindingContext not null, you must unbinding first");
            }

            if(this.BindingContext == bindingContext)
            {
                return;
            }

            this.BindingContext = bindingContext ?? throw new System.Exception($"{Name} binding error: bindingContext is null");
            this.BindingContext.PropertyChanged += OnPropertyChanged;
            Binding(bindingContext);
        }

        /// <summary>
        /// 解绑上下文
        /// </summary>
        void IView.Unbinding()
        {
            if(this.BindingContext == null)
            {
                return;
            }

            Unbinding();
            this.BindingContext.PropertyChanged -= OnPropertyChanged;
            this.BindingContext = null;
        }

        /// <summary>
        /// 激活这个界面
        /// </summary>
        void IView.Enable() => Enable();

        /// <summary>
        /// 反激活这个界面
        /// </summary>
        void IView.Disable() => Disable();

        /// <summary>
        /// 销毁这个界面
        /// </summary>
        void IView.Destroy()
        {
            Unbinding();
            RemoveVisualElements();
            Destroy();
        }
        #endregion

        #region 子类接口
        protected virtual void Binding(ObservableObject bindingContext) { }
        protected virtual void Unbinding() { }

        protected virtual void Enable() { }

        protected virtual void Disable() { }

        protected virtual void Destroy() { }
        #endregion
    }
}
