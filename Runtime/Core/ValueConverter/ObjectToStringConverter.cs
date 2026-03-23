using System;

namespace FUI
{
    /// <summary>
    /// 将一个类型转换成string的模板类
    /// </summary>
    public abstract class ToStringConverter<T> : ValueConverter<T, string>
    {
        public override string Convert(T value)
        {
            return value?.ToString();
        }

        public override T ConvertBack(string value)
        {
            throw new Exception($"can not convert back string:{value} to {typeof(T)}");
        }
    }

    /// <summary>
    /// 将一个object转换成String
    /// </summary>
    public class ObjectToStringConverter : ToStringConverter<object> { }

    /// <summary>
    /// 将一个int转换成string
    /// </summary>
    public class IntToStringConverter : ToStringConverter<int>
    {
        public override int ConvertBack(string value)
        {
            if(!int.TryParse(value, out int result))
            {
                throw new Exception ($"can not convert back string:{value} to int32");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个long转换成string
    /// </summary>
    public class LongToStringConverter : ToStringConverter<long>
    {
        public override long ConvertBack(string value)
        {
            if(!long.TryParse(value, out long result))
            {
                throw new Exception($"can not convert back string:{value} to int64");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个short转换成string
    /// </summary>
    public class ShortToStringConverter : ToStringConverter<short>
    {
        public override short ConvertBack(string value)
        {
            if(!short.TryParse(value, out short result))
            {
                throw new Exception($"can not convert back string:{value} to int16");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个byte转换成string
    /// </summary>
    public class ByteToStringConverter : ToStringConverter<byte>
    {
        public override byte ConvertBack(string value)
        {
            if(!byte.TryParse(value, out byte result))
            {
                throw new Exception($"can not convert back string:{value} to byte");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个float转换成string
    /// </summary>
    public class FloatToStringConverter : ToStringConverter<float>
    {
        public override float ConvertBack(string value)
        {
            if(!float.TryParse(value, out float result))
            {
                throw new Exception($"can not convert back string:{value} to float");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个double转换成string
    /// </summary>
    public class DoubleToStringConverter : ToStringConverter<double>
    {
        public override double ConvertBack(string value)
        {
            if(!double.TryParse(value, out double result))
            {
                throw new Exception($"can not convert back string:{value} to double");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个decimal转换成string
    /// </summary>
    public class DecimalToStringConverter : ToStringConverter<decimal>
    {
        public override decimal ConvertBack(string value)
        {
            if(!decimal.TryParse(value, out decimal result))
            {
                throw new Exception($"can not convert back string:{value} to decimal");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个bool转换成string
    /// </summary>
    public class BoolToStringConverter : ToStringConverter<bool>
    {
        public override bool ConvertBack(string value)
        {
            if(!bool.TryParse(value, out bool result))
            {
                throw new Exception($"can not convert back string:{value} to bool");
            }
            return result;
        }
    }

    /// <summary>
    /// 将一个enum转换成string
    /// </summary>
    public class EnumToStringConverter : ToStringConverter<Enum> { }
}
