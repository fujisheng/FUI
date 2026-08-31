namespace FUI.Binding
{
    /// <summary>
    /// 为 ViewModel 提供基础属性变更事件和受保护的通知入口。
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, propertyName);
        }
    }
}
