﻿using FUI;
using FUI.Bindable;
using FUI.Test;
using FUI.UGUI;
using FUI.UGUI.Control;

using UnityEngine;

namespace Test.List
{
    public class TestListItemViewModel : ViewModel
    {
        [Binding("txt_id", nameof(TextElement.Text), typeof(IntToStringConverter))]
        public int ID { get; set; }

        [Binding("txt_name", nameof(TextElement.Text))]
        public string Name { get; set; }

        [Command("btn_button", nameof(ButtonElement.OnClick))]
        public void OnClick()
        {
            UnityEngine.Debug.Log($"OnClickListItem:{ID}");
        }
    }

    [Binding("TestListView")]
    public class TestListViewModel : ViewModel
    {
        [Binding("Scroll View", nameof(ScrollRectElement.List))]
        public ObservableList<TestListItemViewModel> List { get; set; }

        [Command("btn_Add", nameof(ButtonElement.OnClick))]
        public void OnClickAddItem()
        {
            var id = List.Count + 1;
            List.Add(new TestListItemViewModel() { ID = id, Name = "Test" + (id) });
        }

        [Command("btn_Remove", nameof(ButtonElement.OnClick))]
        public void OnClickRemoveItem()
        {
            if (List.Count > 0)
            {
                List.RemoveAt(List.Count - 1);
            }
        }
    }

    public class TestListBehavior : ViewBehavior<TestListViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.List = new ObservableList<TestListItemViewModel>()
            {
                new TestListItemViewModel() { ID = 1, Name = "Test1" },
                new TestListItemViewModel() { ID = 2, Name = "Test2" },
                new TestListItemViewModel() { ID = 3, Name = "Test3" },
                new TestListItemViewModel() { ID = 4, Name = "Test4" },
                new TestListItemViewModel() { ID = 5, Name = "Test5" },
            };
        }
    }

    public class TestList : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestListView");
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
