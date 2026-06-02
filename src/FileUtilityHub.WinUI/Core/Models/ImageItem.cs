using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileUtilityHub.Core.Models;

public enum ProcessingStatus
{
    Pending,
    Processing,
    Success,
    Warning,
    Error,
    Skipped
}

public sealed class ImageItem : INotifyPropertyChanged
{
    private string? _outputPath;
    private long? _outputSizeBytes;
    private ProcessingStatus _status = ProcessingStatus.Pending;
    private string? _warning;
    private string? _errorMessage;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }

    public string FormattedOriginalSize => FormatSize(OriginalSizeBytes);
    public string FormattedOutputSize => OutputSizeBytes.HasValue ? FormatSize(OutputSizeBytes.Value) : "-";

    public string? OutputPath
    {
        get => _outputPath;
        set { _outputPath = value; OnPropertyChanged(); }
    }

    public long? OutputSizeBytes
    {
        get => _outputSizeBytes;
        set { _outputSizeBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedOutputSize)); }
    }

    public ProcessingStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string? Warning
    {
        get => _warning;
        set { _warning = value; OnPropertyChanged(); }
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
