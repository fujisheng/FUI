using System;

namespace FUI.Binding
{
    /// <summary>
    /// 定义基于运行时类型的双向值转换契约。
    /// </summary>
    public interface IValueConverter
    {
        object Convert(object value, Type targetType);
        object ConvertBack(object value, Type targetType);
    }

    /// <summary>
    /// 定义从 ViewModel 值到 Element 目标值的强类型转换。
    /// </summary>
    public interface IForwardValueConverter<in TValueType, out TTargetType>
    {
        TTargetType Convert(TValueType value);
    }

    /// <summary>
    /// 定义从 Element 目标值回写 ViewModel 值的强类型转换。
    /// </summary>
    public interface IBackwardValueConverter<out TValueType, in TTargetType>
    {
        TValueType ConvertBack(TTargetType value);
    }

    /// <summary>
    /// 组合正向与反向强类型值转换能力。
    /// </summary>
    public interface IValueConverter<TValueType, TTargetType> : IForwardValueConverter<TValueType, TTargetType>, IBackwardValueConverter<TValueType, TTargetType> { }
}
