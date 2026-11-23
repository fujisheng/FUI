using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Presentation;
using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 保存默认隐藏的 View 原型，并通过安全 ProjectionTemplate 创建独立投影。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("FUI/Elements/View Prototype")]
    public class ViewPrototypeElement : RectTransformElement, IContainerElement, IProjectionTemplateElement, IListView, IProjectionLayoutElement, IExternalValueSource
    {
        ProjectionCollection collection;
        BindableProperty<ProjectionLayout> layoutValue;

        /// <summary>用于创建受控投影的只读模板。</summary>
        public BindableProperty<ProjectionTemplate> Template { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Template = CreateProperty(new ProjectionTemplate(CreateProjection, ReadLayout, TryScreenToLocal));
            gameObject.SetActive(false);
        }

        ProjectionCollection Collection => collection ??= Track(new ProjectionCollection(CreateProjection, PlaceBefore));

        public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource => Collection.ItemsSource;

        public BindableProperty<ProjectionLayout> LayoutValue => layoutValue ??= CreateProperty(ReadLayout());

        void IListView.OnAdd(object sender, int? index, object itemData) => collection?.Notify(sender, index, 1);
        void IListView.OnRemove(object sender, int? index, object item) => collection?.Notify(sender, index, -1);
        void IListView.OnReplace(object sender, int? index, object oldItem, object newItem) => collection?.Notify(sender, index);
        void IListView.OnUpdate(object sender) => collection?.Notify(sender);

        void IExternalValueSource.SynchronizeExternalValues()
        {
            if (layoutValue != null)
            {
                layoutValue.Value = ReadLayout();
            }
        }

        protected override void OnRelease()
        {
            collection = null;
            layoutValue = null;
            base.OnRelease();
        }

        static void PlaceBefore(ViewInstance instance, ViewInstance next)
        {
            var item = ((View)instance.View).transform;
            if (next == null)
            {
                item.SetAsLastSibling();
                return;
            }

            var index = ((View)next.View).transform.GetSiblingIndex();
            item.SetSiblingIndex(item.GetSiblingIndex() < index ? index - 1 : index);
        }

        ProjectionLayout ReadLayout()
        {
            var parent = transform.parent as RectTransform;
            return new ProjectionLayout(
                RectTransform.rect.width,
                RectTransform.rect.height,
                parent?.rect.width ?? 0f,
                parent?.rect.height ?? 0f,
                parent?.pivot.x ?? 0.5f,
                parent?.pivot.y ?? 0.5f);
        }

        bool TryScreenToLocal(float screenX, float screenY, out float localX, out float localY)
        {
            var parent = transform.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, new Vector2(screenX, screenY), null, out var localPosition))
            {
                localX = localPosition.x;
                localY = localPosition.y;
                return true;
            }

            localX = 0f;
            localY = 0f;
            return false;
        }

        ProjectionCreation CreateProjection(ObservableObject viewModel)
        {
            var clone = Instantiate(gameObject, transform.parent, false);
            var owner = clone.GetComponent<ViewPrototypeElement>();
            var cloneView = View.CreateProjection(AssetLoader, clone, owner);
            ViewInstance cloneInstance = null;
            try
            {
                cloneInstance = ViewInstance.Create(cloneView, viewModel);
                return new ProjectionCreation(cloneInstance, instance => ReleaseClone(instance, cloneView, clone));
            }
            catch
            {
                cloneInstance?.DestroyProjection();
                cloneView?.Release();
                DestroyClone(clone);
                throw;
            }
        }

        void ReleaseClone(ViewInstance instance, View cloneView, GameObject clone)
        {
            instance?.DestroyProjection();
            cloneView?.Release();
            DestroyClone(clone);
        }

        void DestroyClone(GameObject clone)
        {
            if (AssetLoader != null)
            {
                AssetLoader.DestroyGameObject(clone);
            }
            else
            {
                Utility.DestroyObject(clone);
            }
        }
    }
}
