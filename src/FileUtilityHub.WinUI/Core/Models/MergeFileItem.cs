using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Model for a file in the PDF merge list.
/// Separate from ImageItem because merge has different concerns
/// (order, JPEG prep dimensions) vs compression (CRF, savings%).
/// Shares ProcessingStatus enum and FileStatusBadge control.
/// </summary>
public sealed class MergeFileItem : INotifyPropertyChanged
{
    private int _orderIndex;
    private int? _imageWidth;
    private int? _imageHeight;
    private string? _preparedJpegPath;
    private ProcessingStatus _status = ProcessingStatus.Pending;
    private string? _errorMessage;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }

    public string FormattedSize => FormatSize(OriginalSizeBytes);

    public string DimensionDisplay => ImageWidth.HasValue && ImageHeight.HasValue
        ? $"{ImageWidth}×{ImageHeight}"
        : "—";

    public string StatusDisplay => Status switch
    {
        ProcessingStatus.Pending => "Chờ gộp",
        ProcessingStatus.Processing => "Đang xử lý",
        ProcessingStatus.Success => "Đã gộp",
        ProcessingStatus.Error => "Lỗi",
        ProcessingStatus.Skipped => "Bỏ qua",
        _ => Status.ToString()
    };

    public int OrderIndex
    {
        get => _orderIndex;
        set { _orderIndex = value; OnPropertyChanged(); }
    }

    public int? ImageWidth
    {
        get => _imageWidth;
        set { _imageWidth = value; OnPropertyChanged(); OnPropertyChanged(nameof(DimensionDisplay)); }
    }

    public int? ImageHeight
    {
        get => _imageHeight;
        set { _imageHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(DimensionDisplay)); }
    }

    public string? PreparedJpegPath
    {
        get => _preparedJpegPath;
        set { _preparedJpegPath = value; OnPropertyChanged(); }
    }

    public ProcessingStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1048576)
            return $"{bytes / 1048576.0:F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }
}
