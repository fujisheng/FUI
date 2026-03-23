using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 动态View元素 允许数据为空
    /// </summary>
    public class DynamicViewElement : ViewElement
    {
        public BindableProperty<string> Source { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Source = new BindableProperty<string>(string.Empty, OnUpdateSource);
        }

        protected override void OnUpdateData(ObservableObject oldValue, ObservableObject newValue)
        {
            if(entity == null || newValue == null)
            {
                return;
            }

            //如果这个Entity不为空且要更新的数据类型不是之前的数据类型  则需要销毁之前的Entity
            if (entity != null && newValue.GetType() != oldValue?.GetType())
            {
                entity?.Destroy();
                entity = null;
            }

            if (entity == null || newValue.GetType() != oldValue.GetType())
            {
                view = CreateView() as View;
                entity = UIEntity.Create(view, newValue);
                entity.Enable();
            }
            else
            {
                entity.UpdateViewModel(newValue);
            }
        }

        void OnUpdateSource(string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }

            if(entity != null)
            {
                entity.Destroy();
                entity = null;
            }

            if(newValue == null)
            {
                return;
            }

            view = CreateView() as View;
            entity = UIEntity.Create(view, Data.Value);
            entity.Enable();
        }

        protected override IView CreateView()
        {
            var gameObject = AssetLoader.CreateGameObject(Source.Value);
            gameObject.transform.SetParent(this.transform, false);
            gameObject.transform.localPosition = Vector3.zero;
            return View.Create(AssetLoader, gameObject);
        }

        protected override void OnRelease()
        {
            Data.Dispose();
            Source.Dispose();
            entity?.Destroy();
            entity = null;
            view?.Release();
            view = null;
        }
    }
}