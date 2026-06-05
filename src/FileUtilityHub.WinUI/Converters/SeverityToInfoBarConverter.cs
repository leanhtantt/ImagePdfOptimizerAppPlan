using System;
using FileUtilityHub_WinUI.Core.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace FileUtilityHub_WinUI.Converters;

/// <summary>
/// Maps domain WarningSeverityLevel to WinUI InfoBarSeverity.
/// Keeps the ViewModel free of WinUI types.
/// </summary>
public sealed class SeverityToInfoBarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is WarningSeverityLevel level)
        {
            return level switch
            {
                WarningSeverityLevel.Informational => InfoBarSeverity.Informational,
                WarningSeverityLevel.Success => InfoBarSeverity.Success,
                WarningSeverityLevel.Warning => InfoBarSeverity.Warning,
                WarningSeverityLevel.Error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational
            };
        }
        return InfoBarSeverity.Informational;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
