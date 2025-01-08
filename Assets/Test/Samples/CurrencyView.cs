using FUI.Bindable;
using FUI.UGUI.Control;

namespace FUI.Samples
{
    public class CurrencyItemIdToIcon : ValueConverter<int, string>
    {
        public override string Convert(int value)
        {
            return $"icon_currency_{value}";
        }

        public override int ConvertBack(string value)
        {
            if (value.StartsWith("icon_currency_"))
            {
                return int.Parse(value.Substring("icon_currency_".Length));
            }

            return 0;
        }
    }

    public class CurrencyItemViewModel : ObservableObject
    {
        [Binding("icon", nameof(ImageElement.SpriteSource), converterType: typeof(CurrencyItemIdToIcon))]
        public int Id { get; set; }

        [Binding("count", nameof(TextElement.TextObject))]
        public int Count { get; set; }
    }

    public class CurrencyViewModel : ViewModel
    {
        [Binding("CurrencyView", nameof(StaticListViewElement.List))]
        public ObservableList<CurrencyItemViewModel> Items { get; set; }
    }
}