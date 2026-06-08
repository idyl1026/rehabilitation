using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BingChengAssistant.Helpers;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToYesNoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "✓" : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>患者状态 → 背景色</summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value?.ToString()) switch
        {
            "在院" => new SolidColorBrush(Color.FromRgb(56, 161, 105)),   // 绿
            "已出院" => new SolidColorBrush(Color.FromRgb(107, 122, 141)), // 灰
            "已归档" => new SolidColorBrush(Color.FromRgb(22, 119, 255)),  // 蓝
            "病危" => new SolidColorBrush(Color.FromRgb(229, 62, 62)),    // 红
            _ => new SolidColorBrush(Color.FromRgb(156, 163, 175)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>WarningBrush（在XAML中使用）</summary>
public static class ConverterKeys { }
