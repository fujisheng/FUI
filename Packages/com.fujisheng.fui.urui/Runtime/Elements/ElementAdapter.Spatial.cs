using System.Globalization;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using URUI.Documents;

namespace FUI.Rendering.URUI
{
    public partial class ElementAdapter : IWorldPositionElement, IQuaternionRotationElement, ILocalQuaternionRotationElement, IExternalValueSource
    {
        bool spatialInitialized;
        bool spatialNeedsInitialRead;
        bool readingSpatial;
        bool skipNextSpatialWrite;
        object publishingSpatialProperty;
        int spatialWriteVersion;
        bool spatialSubscribed;
        bool spatialListening;
        System.Action<Document> spatialCallback;
        System.Action<Document> spatialUnloadCallback;
        bool awaitingLayout;
        bool pendingPosition;
        bool pendingWorld;
        bool pendingRotation;
        bool pendingLocalRotation;
        bool pendingScale;
        bool positionAbsoluteSet;
        Quaternion requestedRotation;
        SpatialValue requestedScale;
        SpatialValue pendingValue;
        internal bool HasPendingSpatialWrite => pendingPosition || pendingRotation || pendingScale || awaitingLayout;
        Quaternion spatialRotation = Quaternion.identity;
        Vector3 spatialTranslation;
        BindableProperty<SpatialValue> worldPositionValue;
        public BindableProperty<SpatialValue> WorldPositionValue
        {
            get
            {
                EnsureSpatialValues();
                return worldPositionValue;
            }
        }
        BindableProperty<RotationValue> rotationValue;
        public BindableProperty<RotationValue> RotationValue
        {
            get
            {
                EnsureSpatialValues();
                return rotationValue;
            }
        }
        BindableProperty<RotationValue> localRotationValue;
        public BindableProperty<RotationValue> LocalRotationValue
        {
            get
            {
                EnsureSpatialValues();
                return localRotationValue;
            }
        }

        void EnsureSpatialValues()
        {
            if (spatialInitialized)
            {
                return;
            }
            if (element == null)
            {
                throw new System.ObjectDisposedException(nameof(ElementAdapter));
            }
            sizeValue = SpatialProperty(new ScrollPoint(0, 0), ApplySize);
            localPositionValue = SpatialProperty(SpatialValue.zero, value => SchedulePosition(value, false));
            worldPositionValue = SpatialProperty(SpatialValue.zero, value => SchedulePosition(value, true));
            localScaleValue = SpatialProperty(SpatialValue.one, ApplyScale);
            rotationValue = SpatialProperty(new RotationValue(0, 0, 0, 1), ApplyRotation);
            localRotationValue = SpatialProperty(new RotationValue(0, 0, 0, 1), value =>
            {
                ApplyRotation(value);
                pendingLocalRotation = true;
            });
            spatialInitialized = true;
            spatialNeedsInitialRead = true;
        }

        void ApplyRotation(RotationValue value)
        {
            var length = System.Math.Sqrt((double)value.x * value.x + (double)value.y * value.y + (double)value.z * value.z + (double)value.w * value.w);
            if (double.IsNaN(length) || double.IsInfinity(length) || length == 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(value), "Rotation must be a finite nonzero quaternion.");
            }
            requestedRotation = new Quaternion((float)(value.x / length), (float)(value.y / length), (float)(value.z / length), (float)(value.w / length));
            pendingRotation = true;
            pendingLocalRotation = false;
            SubscribeSpatial();
        }

        void ScheduleScale(SpatialValue value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
            {
                throw new System.ArgumentOutOfRangeException(nameof(value), "Scale must be finite.");
            }
            requestedScale = value;
            pendingScale = true;
            SubscribeSpatial();
        }

        void ApplySpatialTransform(SpatialValue scale)
        {
            if (!float.IsFinite(scale.x) || !float.IsFinite(scale.y) || !float.IsFinite(scale.z))
            {
                throw new System.ArgumentOutOfRangeException(nameof(scale), "Scale must be finite.");
            }
            // 文档 Y 轴向下；反射基变换把旋转轴映射为(-x,y,-z)。
            element.SetTransform(spatialTranslation, new Quaternion(-spatialRotation.x, spatialRotation.y, -spatialRotation.z, spatialRotation.w),
                new Vector3(scale.x, scale.y, scale.z));
        }

        static Quaternion ToSpatialRotation(Quaternion value) => new Quaternion(-value.x, value.y, -value.z, value.w);

        static bool TryWorldRotation(global::URUI.Elements.Element target, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            for (var current = target; current != null; current = current.Parent)
            {
                if (!current.HasLayout || !current.TryGetLocalTransform(out _, out var local))
                {
                    return false;
                }
                rotation = ToSpatialRotation(local) * rotation;
            }
            return true;
        }

        static bool TryWorldRotationFromSnapshots(in global::URUI.Elements.ElementSpatialSnapshot childSnap,
            in global::URUI.Elements.ElementSpatialSnapshot parentSnap, global::URUI.Elements.Element grandParent, out Quaternion rotation)
        {
            if (!childSnap.HasSeparateRotation || !parentSnap.HasSeparateRotation)
            {
                rotation = Quaternion.identity;
                return false;
            }
            rotation = ToSpatialRotation(parentSnap.LogicalRotation) * ToSpatialRotation(childSnap.LogicalRotation);
            for (var current = grandParent; current != null; current = current.Parent)
            {
                if (!current.HasLayout || !current.TryGetLocalTransform(out _, out var local))
                {
                    return false;
                }
                rotation = ToSpatialRotation(local) * rotation;
            }
            return true;
        }

        void ApplyPendingTransform()
        {
            if (!element.TryGetLocalTransform(out var matrix, out var logicalRotation))
            {
                throw new System.NotSupportedException("FUI transform writes require explicit rotation/scale primitives; a matrix has no unique logical rotation.");
            }
            var rotationMatrix = Matrix4x4.Rotate(logicalRotation);
            var unrotated = rotationMatrix.transpose * matrix;
            // 任意剪切/透视没有 RectTransform 的独立 TRS 契约，明确拒绝，避免悄悄破坏作者变换。
            const float tolerance = .0001f;
            if (Mathf.Abs(unrotated.m01) > tolerance || Mathf.Abs(unrotated.m02) > tolerance ||
                Mathf.Abs(unrotated.m10) > tolerance || Mathf.Abs(unrotated.m12) > tolerance ||
                Mathf.Abs(unrotated.m20) > tolerance || Mathf.Abs(unrotated.m21) > tolerance ||
                Mathf.Abs(matrix.m30) > tolerance || Mathf.Abs(matrix.m31) > tolerance || Mathf.Abs(matrix.m32) > tolerance || Mathf.Abs(matrix.m33 - 1) > tolerance)
            {
                throw new System.NotSupportedException("FUI transform writes do not support a sheared or perspective local transform.");
            }
            spatialRotation = ToSpatialRotation(logicalRotation);
            if (pendingRotation)
            {
                if (pendingLocalRotation)
                {
                    spatialRotation = requestedRotation;
                }
                else
                {
                    if (!TryWorldRotation(element.Parent, out var parentRotation))
                    {
                        throw new System.NotSupportedException("The ancestor transform has no unambiguous logical rotation.");
                    }
                    spatialRotation = Quaternion.Inverse(parentRotation) * requestedRotation;
                }
            }
            spatialTranslation = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            var scale = pendingScale ? requestedScale : new SpatialValue(unrotated.m00, unrotated.m11, unrotated.m22);
            ApplySpatialTransform(scale);
            pendingRotation = false;
            pendingScale = false;
            awaitingLayout = true;
        }

        sealed class SpatialBindableProperty<T> : BindableProperty<T>
        {
            readonly ElementAdapter owner;
            internal SpatialBindableProperty(ElementAdapter owner, T initial, System.Func<T, T> normalize) : base(initial, null, normalize)
            {
                this.owner = owner;
            }
            public override T GetValue()
            {
                if (owner.element != null && !owner.readingSpatial)
                {
                    var refreshed = owner.element.Document.FlushPendingGeometry();
                    if (refreshed || owner.spatialNeedsInitialRead)
                    {
                        owner.SynchronizeExternalValues();
                    }
                }
                return base.GetValue();
            }
        }

        BindableProperty<T> SpatialProperty<T>(T initial, System.Action<T> apply)
        {
            BindableProperty<T> property = null;
            property = new SpatialBindableProperty<T>(this, initial, value =>
            {
                // 同值写入也必须把零位置或零尺寸应用到作者布局。
                if (skipNextSpatialWrite)
                {
                    skipNextSpatialWrite = false;
                    return value;
                }
                if (spatialInitialized)
                {
                    // A two-way binding echoes an observed value synchronously. Keep that read silent,
                    // while different values and writes to other properties remain genuine input.
                    if (ReferenceEquals(publishingSpatialProperty, property) && System.Collections.Generic.EqualityComparer<T>.Default.Equals(property.Value, value))
                    {
                        return value;
                    }
                    spatialWriteVersion++;
                    apply(value);
                }
                return value;
            });
            return property;
        }

        Vector2 Pivot => new Vector2(ReadPivot(element, "x"), 1 - ReadPivot(element, "y"));
        internal static float ReadPivot(global::URUI.Elements.Element target, string axis)
        {
            if (target == null)
            {
                return .5f;
            }
            var property = axis == "x" ? "--urui-transform-origin-x" : "--urui-transform-origin-y";
            if (!target.ResolvedStyle.TryGetLength(property, out var length))
            {
                if (string.IsNullOrEmpty(target.ResolvedStyle.Get(property)))
                {
                    return .5f;
                }
                throw new System.NotSupportedException("Spatial coordinates require a resolved CSS transform origin.");
            }
            var value = length.Value;
            if (length.Unit == global::URUI.Elements.LengthUnit.Percent)
            {
                value *= .01f;
            }
            else if (length.Unit == global::URUI.Elements.LengthUnit.Pixel)
            {
                var extent = axis == "x" ? target.Layout.width : target.Layout.height;
                if (!target.HasLayout || extent <= 0)
                {
                    return .5f;
                }
                value /= extent;
            }
            else
            {
                throw new System.NotSupportedException("Spatial coordinates require a resolved CSS transform origin.");
            }
            return axis == "x" ? value : 1 - value;
        }

        void SchedulePosition(SpatialValue value, bool world)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
            {
                throw new System.ArgumentOutOfRangeException(nameof(value), "Spatial position must be finite.");
            }
            pendingValue = value;
            pendingWorld = world;
            pendingPosition = true;
            awaitingLayout = false;
            // Transform 位置独立于布局流，等新布局完成后换算边框和滚动偏移。
            if (!positionAbsoluteSet)
            {
                element.Style.Position = global::URUI.Elements.Position.Absolute;
                positionAbsoluteSet = true;
            }
            SubscribeSpatial();
        }

        void SubscribeSpatial()
        {
            if (!spatialSubscribed)
            {
                SpatialWrites.For(element.Document).Add(this);
                spatialSubscribed = true;
            }
            if (!spatialListening)
            {
                spatialCallback ??= ApplyPendingSpatial;
                spatialUnloadCallback ??= ReleaseSpatialListener;
                element.Document.LayoutUpdated += spatialCallback;
                element.Document.Unloaded += spatialUnloadCallback;
                spatialListening = true;
            }
        }

        void ApplyPendingSpatial(Document document)
        {
            if (element == null || !element.IsValid || !element.IsAttached)
            {
                ReleaseSpatialListener(document);
                return;
            }
            if (!spatialSubscribed)
            {
                return;
            }
            var parent = element.Parent;
            // 隐藏的节点可能保留上次布局的标记，等它重新进入可见布局再换算坐标。
            if (!element.HasLayout || parent == null || !parent.HasLayout || !element.IsVisibleInHierarchy)
            {
                return;
            }
            if (pendingRotation || pendingScale)
            {
                if (SpatialWrites.For(document).HasPendingAncestor(element))
                {
                    return;
                }
                try
                {
                    ApplyPendingTransform();
                }
                catch
                {
                    // 不支持的作者变换只报告本次请求，不能永久阻塞后续布局与子请求。
                    CancelPendingSpatial(document);
                    throw;
                }
                return;
            }
            if (!pendingPosition)
            {
                awaitingLayout = false;
                UnsubscribeSpatial(document);
                return;
            }
            var snap = element.GetSpatialSnapshot();
            var parentSnap = parent.GetSpatialSnapshot();
            var size = snap.Layout.size;
            if (!parent.TryWorldToLocal(element.LocalToWorld(Vector2.Scale(size, Pivot)), out var current))
            {
                return;
            }
            Vector2 target;
            if (pendingWorld)
            {
                if (!parent.TryWorldToLocal(new Vector2(pendingValue.x, Screen.height - pendingValue.y), out target))
                {
                    return;
                }
            }
            else
            {
                target = new Vector2(parentSnap.Layout.width * ReadPivot(parent, "x") + pendingValue.x,
                    parentSnap.Layout.height * (1 - ReadPivot(parent, "y")) - pendingValue.y) - parentSnap.ScrollOffset;
            }
            var delta = target - current;
            if (pendingWorld && delta.sqrMagnitude < .0001f)
            {
                // 至少经过一次后续布局，避免同帧父级位置写入尚未生效就过早完成。
                if (!awaitingLayout || SpatialWrites.For(document).HasPendingAncestor(element))
                {
                    awaitingLayout = true;
                    return;
                }
                pendingPosition = false;
                awaitingLayout = false;
                UnsubscribeSpatial(document);
                return;
            }
            // 保留作者的百分比锚点；把位移写入边距，父尺寸变化后仍按同一锚点布局。
            // 两侧约束同时存在时反向移动另一侧，避免平移改变拉伸节点的尺寸。
            var left = HasOffset("left");
            var right = HasOffset("right");
            var top = HasOffset("top");
            var bottom = HasOffset("bottom");
            if (left)
            {
                element.Style.SetPixels("margin-left", ReadOffset("margin-left", parentSnap.Layout.width, 0) + delta.x);
            }
            if (right)
            {
                element.Style.SetPixels("margin-right", ReadOffset("margin-right", parentSnap.Layout.width, 0) - delta.x);
            }
            if (!left && !right)
            {
                element.Style.SetLeft(snap.Layout.x + delta.x);
            }
            if (top)
            {
                element.Style.SetPixels("margin-top", ReadOffset("margin-top", parentSnap.Layout.height, 0) + delta.y);
            }
            if (bottom)
            {
                element.Style.SetPixels("margin-bottom", ReadOffset("margin-bottom", parentSnap.Layout.height, 0) - delta.y);
            }
            if (!top && !bottom)
            {
                element.Style.SetTop(snap.Layout.y + delta.y);
            }
            pendingPosition = pendingWorld;
            awaitingLayout = true;
        }

        float ReadOffset(string property, float parentSize, float fallback)
        {
            if (!element.ResolvedStyle.TryGetLength(property, out var length))
            {
                return fallback;
            }
            if (length.Unit == global::URUI.Elements.LengthUnit.Pixel)
            {
                return length.Value;
            }
            if (length.Unit == global::URUI.Elements.LengthUnit.Percent)
            {
                return length.Value * parentSize / 100;
            }
            return fallback;
        }
        bool HasOffset(string property)
        {
            if (element.ResolvedStyle.TryGetLength(property, out var length))
            {
                return length.Unit is not (global::URUI.Elements.LengthUnit.Auto or global::URUI.Elements.LengthUnit.Unset);
            }
            // 未解析的作者值仍保留原有约束判定；普通数值路径不生成字符串。
            var value = element.ResolvedStyle.Get(property);
            return !string.IsNullOrEmpty(value) && value is not ("auto" or "none" or "unset");
        }

        public void SynchronizeExternalValues()
        {
            if (!spatialInitialized || readingSpatial || element == null || !element.IsValid || !element.HasLayout || HasPendingSpatialWrite)
            {
                return;
            }
            var parent = element.Parent;
            if (parent == null || !parent.HasLayout)
            {
                return;
            }
            readingSpatial = true;
            try
            {
                var snap = element.GetSpatialSnapshot();
                var parentSnap = parent.GetSpatialSnapshot();
                var size = snap.Layout.size;
                var world = element.LocalToWorld(Vector2.Scale(size, Pivot));
                if (!parent.TryWorldToLocal(world, out var projected))
                {
                    return;
                }
                var local = projected + parentSnap.ScrollOffset;
                // 在任何通知之前读完原生快照，监听者可同步销毁或改写布局。
                var worldValue = new SpatialValue(world.x, Screen.height - world.y, WorldPositionValue.Value.z);
                var localValue = new SpatialValue(local.x - parentSnap.Layout.width * ReadPivot(parent, "x"),
                    parentSnap.Layout.height * (1 - ReadPivot(parent, "y")) - local.y, LocalPositionValue.Value.z);
                var sizeValue = new ScrollPoint(size.x, size.y);
                var hasRotation = TryWorldRotationFromSnapshots(in snap, in parentSnap, parent.Parent, out var worldRotation);
                var rotationValue = new RotationValue(worldRotation.x, worldRotation.y, worldRotation.z, worldRotation.w);
                var localRotation = ToSpatialRotation(snap.LogicalRotation);
                var version = spatialWriteVersion;
                spatialNeedsInitialRead = false;
                PublishSpatial(WorldPositionValue, worldValue);
                if (!CanPublishSpatial(version))
                {
                    return;
                }
                PublishSpatial(LocalPositionValue, localValue);
                if (!CanPublishSpatial(version))
                {
                    return;
                }
                PublishSpatial(SizeValue, sizeValue);
                if (!CanPublishSpatial(version))
                {
                    return;
                }
                if (hasRotation)
                {
                    PublishSpatial(RotationValue, rotationValue);
                }
                if (!CanPublishSpatial(version))
                {
                    return;
                }
                PublishSpatial(LocalRotationValue, new RotationValue(localRotation.x, localRotation.y, localRotation.z, localRotation.w));
            }
            finally
            {
                readingSpatial = false;
            }
        }

        bool CanPublishSpatial(int version) => spatialInitialized && element != null && element.IsValid && spatialWriteVersion == version;

        void PublishSpatial<T>(BindableProperty<T> property, T value)
        {
            // 只跳过此次内部赋值的规范器，通知中的业务写入仍必须生效。
            skipNextSpatialWrite = true;
            var previous = publishingSpatialProperty;
            publishingSpatialProperty = property;
            try
            {
                property.Value = value;
            }
            finally
            {
                skipNextSpatialWrite = false;
                publishingSpatialProperty = previous;
            }
        }

        void UnsubscribeSpatial(Document document)
        {
            SpatialWrites.For(document).Remove(this);
            spatialSubscribed = false;
        }

        void ReleaseSpatialListener(Document document)
        {
            CancelPendingSpatial(document);
            if (!spatialListening)
            {
                return;
            }
            document.LayoutUpdated -= spatialCallback;
            document.Unloaded -= spatialUnloadCallback;
            spatialListening = false;
        }

        internal void CancelPendingSpatial(Document document)
        {
            pendingPosition = false;
            pendingRotation = false;
            pendingScale = false;
            awaitingLayout = false;
            UnsubscribeSpatial(document);
        }

        void ReleaseSpatialValues()
        {
            if (element != null)
            {
                ReleaseSpatialListener(element.Document);
            }
            spatialInitialized = false;
            positionAbsoluteSet = false;
            worldPositionValue?.Dispose();
            rotationValue?.Dispose();
            localRotationValue?.Dispose();
        }
    }
}
