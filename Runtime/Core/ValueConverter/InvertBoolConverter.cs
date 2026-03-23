namespace FUI
{
    public class InvertBoolConverter : ValueConverter<bool, bool>
    {
        public override bool Convert(bool value)
        {
            return !value;
        }
        public override bool ConvertBack(bool value)
        {
            return !value;
        }
    }
}