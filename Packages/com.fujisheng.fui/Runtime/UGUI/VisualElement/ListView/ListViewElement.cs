using FUI.Bindable;

using System;

using UnityEngine;

namespace FUI.UGUI.VisualElement
{
    /// <summary>
    /// 列表视图元素基类
    /// </summary>
    public abstract class ListViewElement : UGUIVisualElement, IContainerElement, IListView
    {
        /// <summary>
        /// 所对应的列表
        /// </summary>
        protected IReadOnlyObservableList<ObservableObject> List;

        public sealed override void UpdateValue(object value)
        {
            this.UpdateValue(value as IReadOnlyObservableList<ObservableObject>);
        }

        void IElement<IReadOnlyObservableList<ObservableObject>>.UpdateValue(IReadOnlyObservableList<ObservableObject> value)
        {
            if (value == List || value == null)
            {
                return;
            }

            this.List = value;
            OnUpdate();
        }

        void IListView.OnAdd(object sender, int? index, object item)
        {
            // 如果index为null或者item为null，表示整个列表都更新了
            if (index == null || item == null)
            {
                OnUpdate();
            }
            else
            {
                OnAdd(index.Value, item as ObservableObject);
            }
        }

        void IListView.OnRemove(object sender, int? index, object item)
        {
            if (index == null || item == null)
            {
                OnUpdate();
            }
            else
            {
                OnRemove(index.Value, item as ObservableObject);
            }
        }

        void IListView.OnReplace(object sender, int? index, object oldItem, object newItem)
        {
            if (index == null || oldItem == null || newItem == null)
            {
                OnUpdate();
            }
            else
            {
                OnReplace(index.Value, oldItem as ObservableObject, newItem as ObservableObject);
            }
        }

        void IListView.OnUpdate(object sender)
        {
            OnUpdate();
        }

        /// <summary>
        /// 当添加一个元素的时候
        /// </summary>
        /// <param name="index">添加的index</param>
        /// <param name="item">添加的item</param>
        protected virtual void OnAdd(int index, ObservableObject item) { }

        /// <summary>
        /// 当移除一个元素的时候
        /// </summary>
        /// <param name="index">移除的index</param>
        /// <param name="item">移除的项</param>
        protected virtual void OnRemove(int index, ObservableObject item) { }

        /// <summary>
        /// 当替换的时候
        /// </summary>
        /// <param name="index">替换的index</param>
        /// <param name="oldItem">替换前的item</param>
        /// <param name="newItem">替换后的item</param>
        protected virtual void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem) { }


        /// <summary>
        /// 当全量更新的时候
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// 创建一个ItemView
        /// </summary>
        /// <param name="itemViewModel">这个Item对应的视图模型</param>
        /// <param name="itemObject">这个Item对应的GameObject</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected UIEntity CreateItemEntity(ObservableObject itemViewModel, GameObject itemObject)
        {
            var itemView = new UGUIView(AssetLoader, itemObject, string.Empty);
            return UIEntity.Create(itemView, itemViewModel);
        }
    }
}