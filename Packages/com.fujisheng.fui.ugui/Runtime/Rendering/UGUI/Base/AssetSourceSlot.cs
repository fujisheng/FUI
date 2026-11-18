using System;
using FUI.Binding;
using FUI.Diagnostics;
using FUI.Resources;
using UnityEngine;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 管理一个可由直接对象或资源 Source 驱动的 Unity 资源槽。
    /// </summary>
    internal sealed class AssetSourceSlot<T> : IDisposable where T : UnityEngine.Object
    {
        readonly IAssetLoader loader;
        readonly string elementName;
        IAssetLease<T> lease;
        BindableProperty<T> asset;
        BindableProperty<string> source;
        bool applyingSource;

        internal AssetSourceSlot(IAssetLoader loader, string elementName)
        {
            this.loader = loader;
            this.elementName = elementName;
        }

        internal void Attach(BindableProperty<T> assetProperty, BindableProperty<string> sourceProperty)
        {
            asset = assetProperty;
            source = sourceProperty;
        }

        internal void OnDirectAssetChanged()
        {
            if (applyingSource)
            {
                return;
            }

            ReplaceLease(null);
            SetSourceWithoutLoading(null);
        }

        internal void OnSourceChanged(string oldValue, string newValue)
        {
            if (applyingSource)
            {
                return;
            }

            if (string.IsNullOrEmpty(newValue))
            {
                ApplyAsset(null);
                ReplaceLease(null);
                return;
            }

            IAssetLease<T> candidateLease = null;
            try
            {
                var candidate = default(T);
                if (loader is ILeaseAssetLoader leaseLoader)
                {
                    candidateLease = leaseLoader.LoadLease<T>(newValue);
                    candidate = candidateLease?.Value;
                }
                else if (loader != null)
                {
                    candidate = loader.Load<T>(newValue);
                }

                if (candidate == null)
                {
                    candidateLease?.Dispose();
                    SetSourceWithoutLoading(oldValue);
                    Log.Instance.LogError($"Element '{elementName}' failed to load {typeof(T).Name} from source '{newValue}'.");
                    return;
                }

                ApplyAsset(candidate);
                ReplaceLease(candidateLease);
            }
            catch (Exception exception)
            {
                candidateLease?.Dispose();
                SetSourceWithoutLoading(oldValue);
                Log.Instance.LogError(
                    $"Element '{elementName}' failed to load {typeof(T).Name} from source '{newValue}': {exception.Message}");
                Log.Instance.LogException(exception);
            }
        }

        void ApplyAsset(T value)
        {
            applyingSource = true;
            try
            {
                asset?.SetValue(value);
            }
            finally
            {
                applyingSource = false;
            }
        }

        void SetSourceWithoutLoading(string value)
        {
            applyingSource = true;
            try
            {
                source?.SetValue(value);
            }
            finally
            {
                applyingSource = false;
            }
        }

        void ReplaceLease(IAssetLease<T> replacement)
        {
            var previous = lease;
            lease = replacement;
            previous?.Dispose();
        }

        public void Dispose()
        {
            ReplaceLease(null);
            asset = null;
            source = null;
        }
    }
}
