namespace FUI.Binding
{
    /// <summary>
    /// 在绑定的正向和反向转换中对布尔值取反。
    /// </summary>
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
