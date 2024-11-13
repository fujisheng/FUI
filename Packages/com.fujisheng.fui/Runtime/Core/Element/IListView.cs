using FUI.Bindable;

namespace FUI
{
    /// <summary>
    /// 适用于列表的视图元素
    /// </summary>
    public interface IListView : IView, IElement
    {
        /// <summary>
        /// 数据
        /// </summary>
        BindableProperty<IReadOnlyObservableList<ObservableObject>> List { get; }

        /// <summary>
        /// 当有元素被添加时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index">被添加的元素的index</param>
        /// <param name="itemData">被添加的元素</param>
        void OnAdd(object sender, int? index, object itemData);

        /// <summary>
        /// 当有元素被移除时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index">被移除的元素index</param>
        /// <param name="item">被移除的元素</param>
        void OnRemove(object sender, int? index, object item);

        /// <summary>
        /// 当有元素被替换时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index">被替换的元素index</param>
        /// <param name="oldItem">被替换的元素</param>
        /// <param name="newItem">替换后的元素</param>
        void OnReplace(object sender, int? index, object oldItem, object newItem);

        /// <summary>
        /// 当整个列表被更新时
        /// </summary>
        void OnUpdate(object sender);
    }
}