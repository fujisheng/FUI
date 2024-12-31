using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 视图行为
    /// </summary>
    internal interface IViewBehavior
    {
        /// <summary>
        /// 可以处理的视图模型类型
        /// </summary>
        internal Type ViewModelType { get; }

        /// <summary>
        /// 更新视图模型
        /// </summary>
        /// <param name="vm">视图模型</param>
        internal bool UpdateViewModel(ObservableObject vm);

        /// <summary>
        /// 当创建这个视图行为的时候
        /// </summary>
        /// <param name="vm">创建时的视图模型</param>
        internal void OnCreate(ObservableObject vm);

        /// <summary>
        /// 当激活这个View的时候
        /// </summary>
        /// <param name="param">打开时的参数</param>
        internal void OnEnable(object param);

        /// <summary>
        /// 当这个界面是当前聚焦的View的时候
        /// </summary>
        internal void OnFocus();

        /// <summary>
        /// 当这个界面失焦的时候
        /// </summary>
        internal void OnUnfocus();

        /// <summary>
        /// 当反激活这个View的时候
        /// </summary>
        internal void OnDisable();

        /// <summary>
        /// 当这个界面销毁的时候
        /// </summary>
        internal void OnDestroy();
    }
}