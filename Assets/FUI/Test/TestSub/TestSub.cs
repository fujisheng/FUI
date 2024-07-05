using FUI;
using FUI.Test;

using System.Threading.Tasks;

using UnityEngine;

namespace Test.Sub
{
    public class SubViewModel : ViewModel
    {
        [Binding("txt_coin")]
        public string Coin { get; set; } = "coin:10";
        [Binding("txt_diamond")]
        public string Diamond { get; set; } = "diamond:100";
        [Binding("txt_ticket")]
        public string Ticket { get; set; } = "ticket:1000";
        [Binding("txt_gold")]
        public string Gold { get; set; } = "gold:10000";
    }

    [Binding("MainView")]
    public class MainViewModel : ViewModel
    {
        [Binding("txt_title")]
        public string Titile { get; set; } = "i am main view title";

        [Binding("CurrencyView")]
        public SubViewModel SubViewModel { get; set; } = new SubViewModel();
    }

    public class MainViewBehavior : ViewBehavior<MainViewModel>
    {
        protected override async void OnOpen(object param)
        {
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
