using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Represents a single PDF/DOCX file in the PDF Split batch.
/// Each item produces a folder of extracted page images.
/// </summary>
public sealed class PdfSplitItem : INotifyPropertyChanged
{
    private ProcessingStatus _status = ProcessingStatus.Pending;
    private int _extractedCount;
    private string? _outputFolderPath;
    private string? _errorMessage;
    private TimeSpan? _elapsedTime;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;

    /// <summary>
    /// File extension without dot, lowercase. e.g. "pdf", "docx"
    /// </summary>
    public string Format { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }

    /// <summary>
    /// Number of pages (determined after loading PDF).
    /// </summary>
    public int? PageCount { get; set; }

    public string FormattedOriginalSize => FormatSize(OriginalSizeBytes);

    public string FormatLabel => Format.ToLowerInvariant() switch
    {
        "pdf" => "PDF",
        "docx" => "DOCX",
        _ => Format.ToUpperInvariant()
    };

    public string PageCountDisplay => PageCount.HasValue ? $"{PageCount.Value} trang" : "—";

    public string StatusDisplay => Status switch
    {
        ProcessingStatus.Pending => "Chờ tách",
        ProcessingStatus.Processing => "Đang tách",
        ProcessingStatus.Success => $"Xong ({ExtractedCount} ảnh)",
        ProcessingStatus.Error => "Lỗi",
        ProcessingStatus.Skipped => "Bỏ qua",
        _ => Status.ToString()
    };

    public string ElapsedDisplay => ElapsedTime.HasValue
        ? ElapsedTime.Value.TotalSeconds < 60
            ? $"{ElapsedTime.Value.TotalSeconds:F1}s"
            : $"{(int)ElapsedTime.Value.TotalMinutes}m {ElapsedTime.Value.Seconds}s"
        : "—";

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

    public int ExtractedCount
    {
        get => _extractedCount;
        set
        {
            _extractedCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    public string? OutputFolderPath
    {
        get => _outputFolderPath;
        set { _outputFolderPath = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
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
