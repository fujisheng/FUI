using System;

namespace FUI
{
    public interface IValueConverter
    {
        object Convert(object value, Type targetType, object param);
        object ConvertBack(object value, Type targetType, object param);
    }

    public interface IValueConverter<TValueType, TTargetType, in TParamType>
    {
        TTargetType Convert(TValueType value, TParamType param);
        TValueType ConvertBack(TTargetType value, TParamType param);
    }
}
