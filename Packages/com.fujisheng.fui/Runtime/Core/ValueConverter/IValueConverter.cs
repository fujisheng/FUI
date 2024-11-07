using System;

namespace FUI
{
    public interface IValueConverter
    {
        object Convert(object value, Type targetType);
        object ConvertBack(object value, Type targetType);
    }

    public interface IForwardValueConverter<in TValueType, out TTargetType>
    {
        TTargetType Convert(TValueType value);
    }

    public interface IBackwardValueConverter<out TValueType, in TTargetType>
    {
        TValueType ConvertBack(TTargetType value);
    }

    public interface IValueConverter<TValueType, TTargetType> : IForwardValueConverter<TValueType, TTargetType>, IBackwardValueConverter<TValueType, TTargetType> { }
}
