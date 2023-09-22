
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
        public Name Name { get; set; } = new Name { firstName = "test", lastName = "Ctor" };

        [ObservableProperty]
        public int ID { get; set; } = 0;

        [ObservableProperty]
        public int Age { get; set; } = 0;

        [ObservableProperty]
        public Action Submit { get; set; } = OnSubmit;

        [ObservableProperty]
        public ObservableList<ItemData> List { get; set; }

        [Command]
        public static void OnSubmit()
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
