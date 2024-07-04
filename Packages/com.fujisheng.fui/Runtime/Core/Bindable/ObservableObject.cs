using System;

namespace FUI.Bindable
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, propertyName);
        }
    }
}
