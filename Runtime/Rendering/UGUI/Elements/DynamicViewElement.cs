using FUI.Binding;
using FUI.Diagnostics;
using FUI.Infrastructure;
using FUI.Resources;
using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 从资源 Source 创建嵌套 View，并独占实例 Lease 与投影生命周期。
    /// </summary>
    [AddComponentMenu("FUI/Elements/Dynamic View")]
    public class DynamicViewElement : NestedViewElement
    {
        IAssetLease<GameObject> instanceLease;
        GameObject instanceObject;
        bool restoringSource;

        /// <summary>动态 View Prefab 的资源键；切换失败时保留当前实例。</summary>
        public BindableProperty<string> Source { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Source = CreateProperty(string.Empty, OnSourceChanged);
            TrackCleanup(ReleaseDynamicView);
        }

        protected override void OnUpdateViewModel(ObservableObject oldValue, ObservableObject newValue)
        {
            if (newValue == null)
            {
                instance?.Disable();
                if (instanceObject != null)
                {
                    // Source 决定视觉实例是否存在；ViewModel 只决定是否建立绑定投影。
                    instanceObject.SetActive(true);
                }
                return;
            }

            if (view == null)
            {
                return;
            }

            if (instance != null && instance.ViewModel.GetType() != newValue.GetType())
            {
                instance.DestroyProjection();
                instance = null;
                view.Release();
                view = View.Create(AssetLoader, instanceObject);
            }

            if (instance == null)
            {
                instanceObject.SetActive(true);
                instance = ViewInstance.Create(view, newValue);
                instance.Enable();
                return;
            }

            instance.UpdateViewModel(newValue);
            instance.Enable();
        }

        void OnSourceChanged(string oldValue, string newValue)
        {
            if (restoringSource)
            {
                return;
            }

            if (string.IsNullOrEmpty(newValue))
            {
                ReleaseDynamicView();
                return;
            }

            IAssetLease<GameObject> candidateLease = null;
            GameObject candidateObject = null;
            View candidateView = null;
            ViewInstance candidateInstance = null;
            try
            {
                if (AssetLoader is ILeaseAssetLoader leaseLoader)
                {
                    candidateLease = leaseLoader.InstantiateLease(newValue);
                    candidateObject = candidateLease?.Value;
                }
                else
                {
                    candidateObject = AssetLoader?.CreateGameObject(newValue);
                }

                if (candidateObject == null)
                {
                    candidateLease?.Dispose();
                    RestoreSource(oldValue);
                    Log.Instance.LogError($"Element '{Name}' failed to instantiate View from source '{newValue}'.");
                    return;
                }

                candidateObject.transform.SetParent(transform, false);
                candidateObject.transform.localPosition = Vector3.zero;
                candidateView = View.Create(AssetLoader, candidateObject);
                if (ViewModel.Value != null)
                {
                    candidateInstance = ViewInstance.Create(candidateView, ViewModel.Value);
                    candidateInstance.Enable();
                }
                else
                {
                    candidateObject.SetActive(true);
                }

                ReleaseDynamicView();
                instanceLease = candidateLease;
                instanceObject = candidateObject;
                view = candidateView;
                instance = candidateInstance;
            }
            catch (System.Exception exception)
            {
                candidateInstance?.DestroyProjection();
                candidateView?.Release();
                if (candidateLease != null)
                {
                    candidateLease.Dispose();
                }
                else if (candidateObject != null)
                {
                    DestroyObject(candidateObject);
                }

                RestoreSource(oldValue);
                Log.Instance.LogError($"Element '{Name}' failed to switch View source to '{newValue}': {exception.Message}");
                Log.Instance.LogException(exception);
            }
        }

        protected override IView CreateView() => view;

        void ReleaseDynamicView()
        {
            instance?.DestroyProjection();
            instance = null;
            view?.Release();
            view = null;

            if (instanceLease != null)
            {
                instanceLease.Dispose();
            }
            else if (instanceObject != null)
            {
                DestroyObject(instanceObject);
            }
            instanceLease = null;
            instanceObject = null;
        }

        void DestroyObject(GameObject target)
        {
            if (AssetLoader != null)
            {
                AssetLoader.DestroyGameObject(target);
            }
            else
            {
                Utility.DestroyObject(target);
            }
        }

        void RestoreSource(string value)
        {
            restoringSource = true;
            try
            {
                Source.SetValue(value);
            }
            finally
            {
                restoringSource = false;
            }
        }
    }
}
