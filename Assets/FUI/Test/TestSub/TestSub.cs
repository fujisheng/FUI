using FUI;
using FUI.Test;
using FUI.UGUI.Control;

using System.Threading.Tasks;

using UnityEngine;

namespace Test.Sub
{
    public class SubViewModel : ViewModel
    {
        [Binding("txt_coin", nameof(TextElement.Text))]
        public string Coin { get; set; } = "coin:10";
        [Binding("txt_diamond", nameof(TextElement.Text))]
        public string Diamond { get; set; } = "diamond:100";
        [Binding("txt_ticket", nameof(TextElement.Text))]
        public string Ticket { get; set; } = "ticket:1000";
        [Binding("txt_gold", nameof(TextElement.Text))]
        public string Gold { get; set; } = "gold:10000";
    }

    [Binding("MainView")]
    public class MainViewModel : ViewModel
    {
        [Binding("txt_title", nameof(TextElement.Text))]
        public string Titile { get; set; } = "i am main view title";

        [Binding("CurrencyView", nameof(ViewElement.Data))]
        public SubViewModel SubViewModel { get; set; } = new SubViewModel();

        [Binding("btn_test", nameof(ButtonElement.TextValue))]
        public string ButtonText { get; set; }
    }

    public class MainViewBehavior : ViewBehavior<MainViewModel>
    {
        protected override async void OnOpen(object param)
        {
            VM.ButtonText = "click me";

            UnityEngine.Debug.Log("data 1.........");
            await Task.Delay(1000);
            UnityEngine.Debug.Log("data 2.........");
            VM.SubViewModel = new SubViewModel
            {
                Coin = "coin:20",
                Diamond = "diamond:200",
                Ticket = "ticket:2000",
                Gold = "gold:20000"
            };

            await Task.Delay(1000);
            UnityEngine.Debug.Log("data 3.........");
            VM.SubViewModel.Coin = "coin:30";
            VM.SubViewModel.Diamond = "diamond:300";
            VM.SubViewModel.Ticket = "ticket:3000";
            VM.SubViewModel.Gold = "gold:30000";
        }
    }

    public class TestSub : MonoBehaviour
    {
        void Start()
        {
            TestLauncher.Instance.UIManager.OpenAsync("MainView");
        }
    }
}
