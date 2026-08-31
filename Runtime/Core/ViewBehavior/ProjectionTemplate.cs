using System;
using FUI.Binding;
using FUI.Infrastructure;

namespace FUI.Presentation
{
    /// <summary>
    /// 表示可创建受控嵌套投影的安全模板，不公开底层 View、Element 或绑定上下文。
    /// </summary>
    public sealed class ProjectionTemplate
    {
        readonly Func<ObservableObject, ProjectionCreation> factory;
        readonly ProjectionPositionConverter positionConverter;

        internal ProjectionTemplate(
            Func<ObservableObject, ProjectionCreation> factory,
            ProjectionLayout layout,
            ProjectionPositionConverter positionConverter)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Layout = layout;
            this.positionConverter = positionConverter;
        }

        /// <summary>模板自身及父级容器的只读布局信息。</summary>
        public ProjectionLayout Layout { get; }

        /// <summary>
        /// 使用指定 ViewModel 创建一个新的投影句柄。
        /// </summary>
        public ProjectionHandle<TViewModel> Create<TViewModel>(TViewModel viewModel)
            where TViewModel : ObservableObject
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            var creation = factory(viewModel);
            return new ProjectionHandle<TViewModel>(creation.Instance, creation.Release);
        }

        /// <summary>将屏幕坐标转换为模板父级容器的局部坐标。</summary>
        public bool TryScreenToLocal(float screenX, float screenY, out float localX, out float localY)
        {
            if (positionConverter == null)
            {
                localX = 0f;
                localY = 0f;
                return false;
            }
            return positionConverter(screenX, screenY, out localX, out localY);
        }
    }

    /// <summary>不依赖渲染后端的投影布局尺寸和枢轴信息。</summary>
    public readonly struct ProjectionLayout
    {
        /// <summary>创建模板和父容器的布局快照。</summary>
        public ProjectionLayout(
            float width,
            float height,
            float parentWidth,
            float parentHeight,
            float parentPivotX,
            float parentPivotY)
        {
            Width = width;
            Height = height;
            ParentWidth = parentWidth;
            ParentHeight = parentHeight;
            ParentPivotX = parentPivotX;
            ParentPivotY = parentPivotY;
        }

        /// <summary>模板宽度。</summary>
        public float Width { get; }

        /// <summary>模板高度。</summary>
        public float Height { get; }

        /// <summary>父容器宽度。</summary>
        public float ParentWidth { get; }

        /// <summary>父容器高度。</summary>
        public float ParentHeight { get; }

        /// <summary>父容器水平方向枢轴。</summary>
        public float ParentPivotX { get; }

        /// <summary>父容器垂直方向枢轴。</summary>
        public float ParentPivotY { get; }

        /// <summary>父容器本地坐标的最小 X。</summary>
        public float ParentXMin => -ParentWidth * ParentPivotX;

        /// <summary>父容器本地坐标的最小 Y。</summary>
        public float ParentYMin => -ParentHeight * ParentPivotY;
    }

    /// <summary>尝试把屏幕坐标转换为投影父容器的局部坐标。</summary>
    internal delegate bool ProjectionPositionConverter(
        float screenX,
        float screenY,
        out float localX,
        out float localY);

    /// <summary>描述 ProjectionTemplate 创建出的内部实例及其释放策略。</summary>
    internal readonly struct ProjectionCreation
    {
        internal ProjectionCreation(ViewInstance instance, Action<ViewInstance> release)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Release = release ?? throw new ArgumentNullException(nameof(release));
        }

        internal ViewInstance Instance { get; }
        internal Action<ViewInstance> Release { get; }
    }
}
