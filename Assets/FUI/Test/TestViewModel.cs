
using FUI.Bindable;

using System;

namespace FUI.Test
{
    public class Name
    {
        public string firstName;
        public string lastName;

        public override string ToString()
        {
            return $"{lastName} {firstName}";
        }
    }

    [ObservableObject]
    public class ItemData : ObservableObject
    {
        [ObservableProperty]
        public int ID { get; set; }
        [ObservableProperty]
        public string Name { get; set; }
    }

    [ObservableObject]
    public partial class TestViewModel : ViewModel
    {
        [ObservableProperty]
        public Name Name { get; set; }

        [ObservableProperty]
        public int ID { get; set; }

        [ObservableProperty]
        public int Age { get; set; }

        [ObservableProperty]
        public Action Submit { get; set; }

        [ObservableProperty]
        public ObservableList<ItemData> List { get; set; }

        //public override void Initialize()
        //{
        //    Name = new Name { firstName = "Test", lastName = "1" };
        //    ID = 0;
        //    Age = 0;
        //    Submit = OnSubmit;
        //    List = new ObservableList<ItemData> { 1, 2, 3 };
        //}

        [Command]
        public virtual void OnSubmit()
        {
            UnityEngine.Debug.Log("ClickBtn TestViewModel....");
        }

        [Command]
        public virtual void OnClickItem(int index)
        {
            UnityEngine.Debug.Log($"ClickItem {index} TestViewModel....");
        }
    }
}
