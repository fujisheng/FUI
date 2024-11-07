using System;

namespace FUI
{
    public interface IValueConverter
    {
        object Convert(object value, Type targetType, object param);
        object ConvertBack(object value, Type targetType, object param);
    }

    public interface IForwardValueConverter<in TValueType, out TTargetType>
    {
        TTargetType Convert(TValueType value);
    }

    public interface IBackwardValueConverter<out TValueType, in TTargetType>
    {
        TValueType ConvertBack(TTargetType value);
    }

    public interface IValueConverter<TValueType, TTargetType, in TParamType> : IForwardValueConverter<TValueType, TTargetType>, IBackwardValueConverter<TValueType, TTargetType>
    {
        TTargetType Convert(TValueType value, TParamType param);
        TValueType ConvertBack(TTargetType value, TParamType param);
    }
}
