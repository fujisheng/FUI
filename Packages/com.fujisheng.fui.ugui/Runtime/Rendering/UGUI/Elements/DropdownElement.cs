using System.Collections.Generic;
using FUI.Binding;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 定义 Legacy 与 TMP Dropdown 共享的绑定契约。
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class DropdownElement : SelectableElement, IDropdownElement
    {
        /// <summary>读取后端当前值。</summary>
        protected abstract int ValueValue { get; }

        /// <summary>读取或写入后端淡出速度。</summary>
        protected abstract float AlphaFadeSpeedValue { get; set; }

        /// <summary>读取后端选项文本。</summary>
        protected abstract IReadOnlyList<string> ReadOptions();

        /// <summary>以不发送 Unity 事件的方式写入当前值。</summary>
        protected abstract void SetValueWithoutNotify(int value);

        /// <summary>替换后端选项文本。</summary>
        protected abstract void ReplaceOptions(IReadOnlyList<string> options);

        /// <summary>订阅后端值变更事件。</summary>
        protected abstract void AddValueChangedListener(UnityAction<int> listener);

        /// <summary>取消订阅后端值变更事件。</summary>
        protected abstract void RemoveValueChangedListener(UnityAction<int> listener);

        /// <summary>当前选项值；TMP 多选模式下使用位掩码语义。</summary>
        public BindableProperty<int> Value { get; private set; }

        /// <summary>选项文本集合。</summary>
        public BindableProperty<IReadOnlyList<string>> Options { get; private set; }

        /// <summary>下拉列表的淡出速度。</summary>
        public BindableProperty<float> AlphaFadeSpeed { get; private set; }

        /// <summary>用户修改选项时触发。</summary>
        public Command<int> OnValueChanged { get; private set; }

        Command<int> IDropdownElement.ValueChanged => OnValueChanged;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Value = CreateProperty(ValueValue, (oldValue, newValue) => SetValueWithoutNotify(newValue));
            Options = CreateProperty(ReadOptions(), (oldValue, newValue) => ReplaceOptions(newValue));
            AlphaFadeSpeed = CreateProperty(AlphaFadeSpeedValue, (oldValue, newValue) => AlphaFadeSpeedValue = newValue);
            OnValueChanged = Track(new Command<int>());
            AddValueChangedListener(HandleValueChanged);
            TrackCleanup(() => RemoveValueChangedListener(HandleValueChanged));
        }

        void HandleValueChanged(int value)
        {
            Value.SetValue(value);
            OnValueChanged.Invoke(value);
        }
    }

    /// <summary>
    /// 为自定义 Dropdown 后端提供强类型组件缓存和 Selectable 适配。
    /// </summary>
    /// <typeparam name="TDropdown">Prefab 中预置的 Dropdown 组件类型。</typeparam>
    public abstract class DropdownElement<TDropdown> : DropdownElement where TDropdown : Selectable
    {
        /// <summary>获取当前 Element 适配的强类型 Dropdown 组件。</summary>
        protected TDropdown Component { get; private set; }

        /// <inheritdoc />
        protected sealed override Selectable SelectableComponent => Component;

        protected override void OnInitialize()
        {
            Component = GetComponent<TDropdown>();
            if (Component == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) requires component '{typeof(TDropdown).FullName}'.");
            }

            TrackCleanup(() => Component = null);
            base.OnInitialize();
        }
    }
}
