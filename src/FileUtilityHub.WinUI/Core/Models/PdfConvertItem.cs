using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Represents a single file in the Convert PDF batch.
/// Each item is an independent conversion job that produces one PDF output.
/// </summary>
public sealed class PdfConvertItem : INotifyPropertyChanged
{
    private ProcessingStatus _status = ProcessingStatus.Pending;
    private string? _outputPath;
    private long? _outputSizeBytes;
    private string? _errorMessage;
    private string? _warning;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;

    /// <summary>
    /// File extension without dot, lowercase. e.g. "pdf", "docx", "jpg"
    /// </summary>
    public string Format { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }

    public string FormattedOriginalSize => FormatSize(OriginalSizeBytes);
    public string FormattedOutputSize => OutputSizeBytes.HasValue ? FormatSize(OutputSizeBytes.Value) : "—";

    /// <summary>
    /// Display-friendly format category for the ListView.
    /// </summary>
    public string FormatCategory => Format.ToLowerInvariant() switch
    {
        "pdf" => "PDF",
        "doc" or "docx" => "Word",
        "xls" or "xlsx" => "Excel",
        "ppt" or "pptx" => "PowerPoint",
        "jpg" or "jpeg" or "png" or "bmp" or "tif" or "tiff" or "webp" or "avif" => "Ảnh",
        _ => Format.ToUpperInvariant()
    };

    public string StatusDisplay => Status switch
    {
        ProcessingStatus.Pending => "Chờ convert",
        ProcessingStatus.Processing => "Đang xử lý",
        ProcessingStatus.Success => "Thành công",
        ProcessingStatus.Warning => "Cảnh báo",
        ProcessingStatus.Error => "Lỗi",
        ProcessingStatus.Skipped => "Bỏ qua",
        _ => Status.ToString()
    };

    public string ResultDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                return ErrorMessage!;
            if (!string.IsNullOrWhiteSpace(Warning))
                return Warning!;
            if (!string.IsNullOrWhiteSpace(OutputPath))
                return Path.GetFileName(OutputPath);
            return "—";
        }
    }

    public bool HasOutput => OutputSizeBytes.HasValue;

    public ProcessingStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public string? OutputPath
    {
        get => _outputPath;
        set
        {
            _outputPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public long? OutputSizeBytes
    {
        get => _outputSizeBytes;
        set
        {
            _outputSizeBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedOutputSize));
            OnPropertyChanged(nameof(HasOutput));
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public string? Warning
    {
        get => _warning;
        set
        {
            _warning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ResultDisplay));
        }
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
