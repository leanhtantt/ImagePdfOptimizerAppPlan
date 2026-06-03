using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FileUtilityHub_WinUI.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isVisible = value is bool boolValue && boolValue;
        if (parameter is string text && text.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            isVisible = !isVisible;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}
