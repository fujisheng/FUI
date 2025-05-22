namespace FUI
{
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