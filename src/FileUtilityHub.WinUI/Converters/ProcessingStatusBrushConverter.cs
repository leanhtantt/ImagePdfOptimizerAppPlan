using System;
using FileUtilityHub.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace FileUtilityHub_WinUI.Converters;

public sealed class ProcessingStatusBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var resourceKey = value switch
        {
            ProcessingStatus.Processing => "FileStatusProcessingBrush",
            ProcessingStatus.Success => "FileStatusSuccessBrush",
            ProcessingStatus.Warning => "FileStatusWarningBrush",
            ProcessingStatus.Error => "FileStatusErrorBrush",
            ProcessingStatus.Skipped => "FileStatusSkippedBrush",
            _ => "FileStatusPendingBrush"
        };

        return Application.Current.Resources[resourceKey] as Brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
