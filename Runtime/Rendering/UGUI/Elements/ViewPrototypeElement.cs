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
    public class ViewPrototypeElement : RectTransformElement, IContainerElement
    {
        /// <summary>用于创建受控投影的只读模板。</summary>
        public BindableProperty<ProjectionTemplate> Template { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            var parent = transform.parent as RectTransform;
            var layout = new ProjectionLayout(
                RectTransform.rect.width,
                RectTransform.rect.height,
                parent?.rect.width ?? 0f,
                parent?.rect.height ?? 0f,
                parent?.pivot.x ?? 0.5f,
                parent?.pivot.y ?? 0.5f);
            Template = CreateProperty(new ProjectionTemplate(CreateProjection, layout, TryScreenToLocal));
            gameObject.SetActive(false);
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
