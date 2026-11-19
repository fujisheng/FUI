using System;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using global::URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 基础适配器：名字、可见性、可交互与着色。
    /// </summary>
    public partial class ElementAdapter : IElement, IVisibilityElement, IInteractableElement, IColorElement, ISizeElement, ILocalPositionElement, ILocalScaleElement, IDisposable
    {
        readonly string name;
        global::URUI.Elements.Element element;
        bool interactable = true;
        readonly VisibilityLease visibility;

        static readonly Unity.Profiling.ProfilerMarker constructMarker = new("FUI.URUI.Element.Construct");
        public ElementAdapter(global::URUI.Elements.Element element, string name)
        {
            using var sample = constructMarker.Auto();
            this.element = element ?? throw new ArgumentNullException(nameof(element));
            this.name = string.IsNullOrEmpty(name) ? element.Name : name;

            visibility = new VisibilityLease(element);
            Visible = visibility.Property;
            Interactable = new BindableProperty<bool>(element.EnabledInHierarchy, (_, value) => ApplyInteractable(value));
            Tint = new BindableProperty<Rgba>(new Rgba(1f, 1f, 1f, 1f), (_, value) => ApplyTint(value));
        }

        public string Name => name;

        public IElement Parent => null;

        public BindableProperty<bool> Visible
        {
            get;
        }

        public BindableProperty<bool> Interactable
        {
            get;
        }

        public BindableProperty<Rgba> Tint
        {
            get;
        }
        BindableProperty<ScrollPoint> sizeValue;
        public BindableProperty<ScrollPoint> SizeValue
        {
            get
            {
                EnsureSpatialValues();
                return sizeValue;
            }
        }
        BindableProperty<SpatialValue> localPositionValue;
        public BindableProperty<SpatialValue> LocalPositionValue
        {
            get
            {
                EnsureSpatialValues();
                return localPositionValue;
            }
        }
        BindableProperty<SpatialValue> localScaleValue;
        public BindableProperty<SpatialValue> LocalScaleValue
        {
            get
            {
                EnsureSpatialValues();
                return localScaleValue;
            }
        }

        public global::URUI.Elements.Element Element => element;

        void ApplyInteractable(bool value)
        {
            interactable = value;
            element.SetEnabled(value);
        }

        protected virtual void ApplyTint(Rgba value)
        {
            try
            {
                var color = new UnityEngine.Color(value.r, value.g, value.b, value.a);
                if (element is global::URUI.Controls.Image)
                {
                    element.Style.SetColor("--urui-image-color", color);
                }
                else
                {
                    element.Style.SetColor("color", color);
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"URUI tint skipped on '{name}': {exception.Message}");
            }
        }

        void ApplySize(ScrollPoint value)
        {
            element.Style.SetWidth(value.x);
            element.Style.SetHeight(value.y);
        }

        void ApplyScale(SpatialValue value)
        {
            ScheduleScale(value);
        }

        public virtual void Dispose()
        {
            if (element == null)
            {
                return;
            }
            ReleaseSpatialValues();
            localScaleValue?.Dispose();
            sizeValue?.Dispose();
            localPositionValue?.Dispose();
            visibility.Dispose();
            Interactable?.Dispose();
            Tint?.Dispose();
            element = null;
        }
    }
}
