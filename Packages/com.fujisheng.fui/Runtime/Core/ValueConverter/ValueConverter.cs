using System;

namespace FUI
{
    /// <summary>
    /// 值转换器
    /// </summary>
    /// <typeparam name="TValueType">值类型</typeparam>
    /// <typeparam name="TTargetType">目标类型</typeparam>
    public abstract class ValueConverter<TValueType, TTargetType> : IValueConverter, IValueConverter<TValueType, TTargetType>
    {
        /// <summary>
        /// 将一个值转换成另一个类型
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public abstract TTargetType Convert(TValueType value);
        /// <summary>
        /// 将一个值转换成另一个类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract TValueType ConvertBack(TTargetType value);

        TTargetType IForwardValueConverter<TValueType, TTargetType>.Convert(TValueType value)
        {
            return Convert(value);
        }

        TValueType IBackwardValueConverter<TValueType, TTargetType>.ConvertBack(TTargetType value)
        {
            return ConvertBack(value);
        }

        public object Convert(object value, Type targetType)
        {
            if (targetType != typeof(TTargetType))
            {
                throw new Exception($"Target type:{targetType} is not {typeof(TTargetType).Name}");
            }
            return Convert((TValueType)value);
        }

        public object ConvertBack(object value, Type targetType)
        {
            if (targetType != typeof(TValueType))
            {
                throw new Exception($"Target type:{targetType} is not {typeof(TValueType).Name}");
            }
            return ConvertBack((TTargetType)value);
        }
    }
}
