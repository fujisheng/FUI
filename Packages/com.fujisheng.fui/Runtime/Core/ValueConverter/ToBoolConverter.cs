namespace FUI.Binding
{
    /// <summary>将整数的非零状态与布尔值双向转换。</summary>
    public class IntToBoolConverter : ValueConverter<int, bool>
    {
        public override bool Convert(int value)
        {
            return value != 0;
        }

        public override int ConvertBack(bool value)
        {
            return value ? 1 : 0;
        }
    }

    /// <summary>将单精度浮点数的非零状态与布尔值双向转换。</summary>
    public class FloatToBoolConverter : ValueConverter<float, bool>
    {
        public override bool Convert(float value)
        {
            return value != 0;
        }
        public override float ConvertBack(bool value)
        {
            return value ? 1 : 0;
        }
    }

    /// <summary>将双精度浮点数的非零状态与布尔值双向转换。</summary>
    public class DoubleToBoolConverter : ValueConverter<double, bool>
    {
        public override bool Convert(double value)
        {
            return value != 0;
        }
        public override double ConvertBack(bool value)
        {
            return value ? 1 : 0;
        }
    }

    /// <summary>将字符串的非空状态与布尔值双向转换。</summary>
    public class StringToBoolConverter : ValueConverter<string, bool>
    {
        public override bool Convert(string value)
        {
            return !string.IsNullOrEmpty(value);
        }
        public override string ConvertBack(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
