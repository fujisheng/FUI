using FUI.Bindable;
using FUI.Manager;
using FUI.UGUI.Control;

namespace FUI.Samples
{
    [Config(Layer.Common, Attributes.FullScreen)]
    [Binding("HomeView")]
    public class HomeViewModel : ViewModel
    {
        [Binding("userIcon", nameof(ImageElement.SpriteSource))]
        public string UserIcon { get; set; }

        [Binding("userName", nameof(TextElement.Text))]
        public string UserName { get; set; }

        [Binding("CurrencyView", nameof(ViewElement.Data))]
        public CurrencyViewModel Currency { get; set; }

        [Command("taskButton", nameof(ButtonElement.OnClick))]
        public void OnTaskClick()
        {

        }

        [Command("shopButton", nameof(ButtonElement.OnClick))]
        public void OnShopClick()
        {

        }

        [Command("rankingButton", nameof(ButtonElement.OnClick))]
        public void OnRankingClick()
        {

        }

        [Command("playButton", nameof(ButtonElement.OnClick))]
        public void OnPlayClick()
        {

        }

        [Command("challengeButton", nameof(ButtonElement.OnClick))]
        public void OnChallengeClick()
        {

        }

        [Command("mapButton", nameof(ButtonElement.OnClick))]
        public void OnMapClick()
        {

        }
    }

    public class HomeViewBehavior : ViewBehavior<HomeViewModel>
    {
        protected override void OnCreate()
        {
            VM.UserIcon = "icon_user_1";
            VM.UserName = "FUI";
            VM.Currency = new CurrencyViewModel
            {
                Items = new ObservableList<CurrencyItemViewModel>
                {
                    new CurrencyItemViewModel { Id = 1, Count = 100 },
                    new CurrencyItemViewModel { Id = 2, Count = 200 },
                    new CurrencyItemViewModel { Id = 3, Count = 300 },
                }
            };
        }
    }
}