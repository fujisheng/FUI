﻿using FUI;
using FUI.Bindable;
using FUI.Test;
using FUI.UGUI;

using UnityEngine;

namespace Test.List
{
    public class TestListItemViewModel : ViewModel
    {
        [Binding("txt_id", converterType:typeof(IntToStringConverter))]
        public int ID { get; set; }

        [Binding("txt_name")]
        public string Name { get; set; }
    }

    [Binding("TestListView")]
    public class TestListViewModel : ViewModel
    {
        [Binding("Scroll View")]
        public ObservableList<TestListItemViewModel> List { get; set; }

        [Binding("btn_Add")]
        public System.Action AddItem { get; set; }

        [Binding("btn_Remove")]
        public System.Action RemoveItem { get; set; }
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

            VM.AddItem = OnAddItem;
            VM.RemoveItem = OnRemoveItem;
        }

        void OnAddItem()
        {
            var id = VM.List.Count + 1;
            VM.List.Add(new TestListItemViewModel() { ID = id, Name = "Test" + (id) });
        }

        void OnRemoveItem()
        {
            if (VM.List.Count > 0)
            {
                VM.List.RemoveAt(VM.List.Count - 1);
            }
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
