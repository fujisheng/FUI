namespace FUI.Binding
{
    /// <summary>处理对象属性名称变更通知。</summary>
    public delegate void PropertyChangedHandler(object sender, string propertyName);
    /// <summary>处理强类型属性值变更通知，并提供变更前后的值。</summary>
    public delegate void PropertyChangedHandler<in T>(object sender, T preValue, T newValue);
    /// <summary>定义对象属性变更通知契约。</summary>
    public interface INotifyPropertyChanged
    {
        event PropertyChangedHandler PropertyChanged;
    }

    /// <summary>处理可观察集合的元素新增通知。</summary>
    public delegate void CollectionAddHandler(object sender, int? index, object item);
    /// <summary>处理可观察集合的元素移除通知。</summary>
    public delegate void CollectionRemoveHandler(object sender, int? index, object item);
    /// <summary>处理可观察集合的元素替换通知。</summary>
    public delegate void CollectionReplaceHandler(object sender, int? index, object oldItem, object newItem);
    /// <summary>处理可观察集合的整体刷新通知。</summary>
    public delegate void CollectionUpdateHandler(object sender);

    /// <summary>定义可观察集合的增删、替换和整体刷新通知契约。</summary>
    public interface INotifyCollectionChanged
    {
        event CollectionAddHandler CollectionAdd;
        event CollectionRemoveHandler CollectionRemove;
        event CollectionReplaceHandler CollectionReplace;
        event CollectionUpdateHandler CollectionUpdate;
    }
}
