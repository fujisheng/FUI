using System;
using FUI.Binding;
using FUI.Rendering.UGUI;
using TMPro;
using UnityEngine;

namespace FUI.RedDot.UGUI
{
    /// <summary>红点视觉的显示模式。</summary>
    public enum RedDotVisualMode
    {
        /// <summary>只显示圆点。</summary>
        Dot,
        /// <summary>始终显示数量。</summary>
        Count,
        /// <summary>数量为 1 显示圆点，大于 1 显示数量。</summary>
        Auto,
    }

    /// <summary>把一个强类型 Key 的已提交状态映射到当前 Prefab 自己的红点视觉。</summary>
    public class RedDotElement : RectTransformElement
    {
        [SerializeField] protected GameObject visualRoot;
        [SerializeField] protected TMP_Text countLabel;
        [SerializeField] protected RedDotVisualMode mode = RedDotVisualMode.Auto;
        [SerializeField, Min(1)] protected int countLimit = 99;

        IDisposable subscription;
        IRedDotReader reader;
        RedDotScope scope;
        bool initialized;
        bool contextAttached;
        bool runtimeContextAttached;

        /// <summary>当前绑定节点；变化后会精确切换订阅。</summary>
        public BindableProperty<RedDotKey> Key { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (visualRoot == null) throw new MissingReferenceException($"RedDotElement '{name}' requires a child visual root.");
            if (visualRoot == gameObject) throw new InvalidOperationException($"RedDotElement '{name}' cannot use its own GameObject as visual root.");

            Key = CreateProperty<RedDotKey>(default, OnKeyChanged);
            initialized = true;
            TrackCleanup(() =>
            {
                initialized = false;
                DetachContext();
                Unsubscribe();
            });
            HideVisual();
            if (isActiveAndEnabled) ResolveContext();
        }

        protected override void OnRelease()
        {
            DetachContext();
            Unsubscribe();
            base.OnRelease();
        }

        protected virtual void OnEnable()
        {
            if (initialized) ResolveContext();
        }

        protected virtual void OnDisable()
        {
            DetachContext();
            Unsubscribe();
        }

        protected virtual void OnTransformParentChanged()
        {
            if (initialized && isActiveAndEnabled) ResolveContext();
        }

        void OnKeyChanged(RedDotKey oldKey, RedDotKey newKey)
        {
            Unsubscribe();
            HideVisual();
            TrySubscribe();
        }

        void ResolveContext()
        {
            DetachContext();
            Unsubscribe();
            // Reader 断开时也必须清除旧账号的视觉；新订阅会立即回放已提交状态。
            HideVisual();

            // 最近父级 Scope 是显式边界；即使未配置，也不能穿透到默认上下文。
            scope = FindNearestScope();
            if (scope != null)
            {
                scope.ReaderChanged += OnContextChanged;
                contextAttached = true;
                reader = scope.Reader;
            }
            else
            {
                RedDotRuntime.ContextChanged += OnContextChanged;
                contextAttached = true;
                runtimeContextAttached = true;
                RedDotRuntime.TryGetReader(out reader);
            }
            TrySubscribe();
        }

        void OnContextChanged()
        {
            if (initialized && isActiveAndEnabled) ResolveContext();
        }

        void DetachContext()
        {
            if (!contextAttached) return;
            if (runtimeContextAttached) RedDotRuntime.ContextChanged -= OnContextChanged;
            else if (!ReferenceEquals(scope, null)) scope.ReaderChanged -= OnContextChanged;
            contextAttached = false;
            runtimeContextAttached = false;
            scope = null;
            reader = null;
        }

        RedDotScope FindNearestScope()
        {
            // 销毁中的 Scope 仍可能暂时存在于组件列表中，必须跳过它并继续寻找外层边界。
            var current = transform;
            while (current != null)
            {
                var candidate = current.GetComponent<RedDotScope>();
                if (candidate != null && candidate.IsAvailable) return candidate;
                current = current.parent;
            }
            return null;
        }

        void TrySubscribe()
        {
            if (!initialized || !isActiveAndEnabled || subscription != null || reader == null || !Key.Value.IsValid) return;
            subscription = reader.Subscribe(Key.Value, ApplyState);
        }

        void Unsubscribe()
        {
            subscription?.Dispose();
            subscription = null;
        }

        void HideVisual()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
            if (countLabel != null) countLabel.gameObject.SetActive(false);
        }

        /// <summary>应用已提交状态；子类可增加动画或皮肤，但不得反向修改框架状态。</summary>
        protected virtual void ApplyState(RedDotState state)
        {
            if (visualRoot == null) return;
            visualRoot.SetActive(state.IsVisible);
            if (!state.IsVisible || countLabel == null) return;

            var showCount = mode == RedDotVisualMode.Count || mode == RedDotVisualMode.Auto && state.Count > 1;
            countLabel.gameObject.SetActive(showCount);
            if (!showCount) return;
            countLabel.text = state.Count > countLimit ? countLimit + "+" : state.Count.ToString();
        }
    }
}
