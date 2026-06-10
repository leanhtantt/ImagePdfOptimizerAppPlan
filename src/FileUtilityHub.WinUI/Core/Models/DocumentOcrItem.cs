using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Represents a single file in the Document OCR batch.
/// Each item is an independent OCR job that produces one DOCX output.
/// </summary>
public sealed class DocumentOcrItem : INotifyPropertyChanged
{
    private OcrProcessingStatus _status = OcrProcessingStatus.Pending;
    private string? _outputPath;
    private long? _outputSizeBytes;
    private string? _errorMessage;
    private TimeSpan? _elapsedTime;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;

    /// <summary>
    /// File extension without dot, lowercase. e.g. "pdf", "jpg"
    /// </summary>
    public string Format { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }

    /// <summary>
    /// Number of pages if determinable (PDF only).
    /// </summary>
    public int? PageCount { get; init; }

    public string FormattedOriginalSize => FormatSize(OriginalSizeBytes);
    public string FormattedOutputSize => OutputSizeBytes.HasValue ? FormatSize(OutputSizeBytes.Value) : "—";

    /// <summary>
    /// Display-friendly format category.
    /// </summary>
    public string FormatCategory => Format.ToLowerInvariant() switch
    {
        "pdf" => "PDF",
        "jpg" or "jpeg" or "png" or "bmp" or "tif" or "tiff" or "webp" => "Ảnh",
        _ => Format.ToUpperInvariant()
    };

    public string StatusDisplay => Status switch
    {
        OcrProcessingStatus.Pending => "Chờ OCR",
        OcrProcessingStatus.Uploading => "Đang gửi",
        OcrProcessingStatus.Processing => "Đang OCR",
        OcrProcessingStatus.BuildingDocx => "Đang tạo Word",
        OcrProcessingStatus.Success => "Thành công",
        OcrProcessingStatus.Error => "Lỗi",
        OcrProcessingStatus.Cancelled => "Đã hủy",
        _ => Status.ToString()
    };

    /// <summary>
    /// Maps OCR-specific status to shared ProcessingStatus for FileStatusBadge.
    /// </summary>
    public ProcessingStatus DisplayStatus => Status switch
    {
        OcrProcessingStatus.Pending => ProcessingStatus.Pending,
        OcrProcessingStatus.Uploading => ProcessingStatus.Processing,
        OcrProcessingStatus.Processing => ProcessingStatus.Processing,
        OcrProcessingStatus.BuildingDocx => ProcessingStatus.Processing,
        OcrProcessingStatus.Success => ProcessingStatus.Success,
        OcrProcessingStatus.Error => ProcessingStatus.Error,
        OcrProcessingStatus.Cancelled => ProcessingStatus.Skipped,
        _ => ProcessingStatus.Pending
    };

    public string ResultDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                return ErrorMessage!;
            if (!string.IsNullOrWhiteSpace(OutputPath))
                return Path.GetFileName(OutputPath);
            return "—";
        }
    }

    public string ElapsedDisplay => ElapsedTime.HasValue
        ? ElapsedTime.Value.TotalSeconds < 60
            ? $"{ElapsedTime.Value.TotalSeconds:F1}s"
            : $"{(int)ElapsedTime.Value.TotalMinutes}m {ElapsedTime.Value.Seconds}s"
        : "—";

    public bool HasOutput => OutputSizeBytes.HasValue;

    public OcrProcessingStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(DisplayStatus));
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

    public TimeSpan? ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            _elapsedTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ElapsedDisplay));
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

/// <summary>
/// Processing status specific to OCR workflow with more granular stages.
/// </summary>
public enum OcrProcessingStatus
{
    Pending,
    Uploading,
    Processing,
    BuildingDocx,
    Success,
    Error,
    Cancelled
}
