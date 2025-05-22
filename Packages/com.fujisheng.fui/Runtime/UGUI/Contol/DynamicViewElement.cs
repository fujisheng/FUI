using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 动态View元素 允许数据为空
    /// </summary>
    public class DynamicViewElement : ViewElement
    {
        UIEntity entity;
        public BindableProperty<string> Source { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Source = new BindableProperty<string>(string.Empty, OnUpdateSource);
        }

        protected override void OnUpdateData(ObservableObject oldValue, ObservableObject newValue)
        {
            if(entity == null || string.IsNullOrEmpty(Source.Value))
            {
                return;
            }

            base.OnUpdateData(oldValue, newValue);
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

            var view = CreateView();
            entity = UIEntity.Create(view, Data.Value);
            entity.Enable();
        }

        protected override IView CreateView()
        {
            var gameObject = AssetLoader.CreateGameObject(Source.Value);
            gameObject.transform.SetParent(this.transform);
            gameObject.transform.localPosition = Vector3.zero;
            return View.Create(AssetLoader, gameObject, string.Empty);
        }

        protected override void OnRelease()
        {
            Data.Dispose();
            Source.Dispose();

            entity?.Destroy();
            entity = null;
        }
    }
}