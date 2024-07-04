using FUI;
using FUI.Test;

using UnityEngine;

namespace Test.EditorBinding
{
    [ObservableObject]
    public class TestEditorBindingViewModel : ViewModel
    {
        [ObservableProperty]
        public string Title { get; set; } = "title";
        [ObservableProperty]
        public string Content { get; set; } = "content";
        [ObservableProperty]
        public int page { get; set; } = 0;
    }

    public class TestEditorBindingViewBehavior : ViewBehavior<TestEditorBindingViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.Title = "TestEditorBindingTitle";
            VM.Content = "TestEditorBindingContent";
            VM.page = 1;
        }
    }

    public class TestEditorBinding : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("TestEditorBindingView");
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
