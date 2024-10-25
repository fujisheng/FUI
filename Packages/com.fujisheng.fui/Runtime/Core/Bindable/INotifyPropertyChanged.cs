namespace FUI.Bindable
{
    public delegate void PropertyChangedHandler(object sender, string propertyName);
    public delegate void PropertyChangedHandler<T>(object sender, T preValue, T newValue);
    public interface INotifyPropertyChanged
    {
        event PropertyChangedHandler PropertyChanged;
    }

    public delegate void CollectionAddHandler(object sender, int? index, object item);
    public delegate void CollectionRemoveHandler(object sender, int? index, object item);
    public delegate void CollectionReplaceHandler(object sender, int? index, object oldItem, object newItem);
    public delegate void CollectionUpdateHandler(object sender);

    public interface INotifyCollectionChanged
    {
        event CollectionAddHandler CollectionAdd;
        event CollectionRemoveHandler CollectionRemove;
        event CollectionReplaceHandler CollectionReplace;
        event CollectionUpdateHandler CollectionUpdate;
    }
}
