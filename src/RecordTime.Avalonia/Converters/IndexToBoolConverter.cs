using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace RecordTime.Avalonia.Converters;

/// <summary>
/// 索引到布尔值转换器，用于 RadioButton 组绑定到索引值
/// </summary>
public class IndexToBoolConverter : IValueConverter
{
    public static readonly IndexToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentIndex && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return currentIndex == targetIndex;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return targetIndex;
        }
        return global::Avalonia.Data.BindingNotification.UnsetValue;
    }
}
