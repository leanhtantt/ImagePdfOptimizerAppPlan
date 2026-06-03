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
    private int? _avifCrfUsed;
    private int? _maxLongEdgeUsed;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }

    public string FormattedOriginalSize => FormatSize(OriginalSizeBytes);
    public string FormattedOutputSize => OutputSizeBytes.HasValue ? FormatSize(OutputSizeBytes.Value) : "-";
    public long? SavedBytes => OutputSizeBytes.HasValue ? OriginalSizeBytes - OutputSizeBytes.Value : null;
    public double? SavedPercent => OutputSizeBytes.HasValue && OriginalSizeBytes > 0
        ? (SavedBytes!.Value / (double)OriginalSizeBytes) * 100
        : null;
    public string OutputFileName => string.IsNullOrWhiteSpace(OutputPath) ? string.Empty : Path.GetFileName(OutputPath);
    public string OutputFolder => string.IsNullOrWhiteSpace(OutputPath) ? string.Empty : Path.GetDirectoryName(OutputPath) ?? string.Empty;
    public string SizeComparison => OutputSizeBytes.HasValue
        ? $"{FormattedOriginalSize} -> {FormattedOutputSize}"
        : FormattedOriginalSize;
    public string SavingsSummary
    {
        get
        {
            var savedBytes = SavedBytes;
            var savedPercent = SavedPercent;
            if (!savedBytes.HasValue || !savedPercent.HasValue)
                return string.Empty;

            var sign = savedBytes.Value >= 0 ? "-" : "+";
            return $"{sign}{Math.Abs(savedPercent.Value):F0}%";
        }
    }
    public string CompressionDetail
    {
        get
        {
            var crf = AvifCrfUsed.HasValue ? $"CRF {AvifCrfUsed.Value}" : "CRF chưa chạy";
            var maxLongEdge = MaxLongEdgeUsed.GetValueOrDefault();
            var edge = maxLongEdge > 0
                ? $"max edge {maxLongEdge}px"
                : "giữ kích thước gốc";
            return $"{crf} | {edge}";
        }
    }
    public string StatusDisplay => Status switch
    {
        ProcessingStatus.Pending => "Chờ nén",
        ProcessingStatus.Processing => "Đang nén",
        ProcessingStatus.Success => "Đã nén",
        ProcessingStatus.Warning => "Cần xem lại",
        ProcessingStatus.Error => "Lỗi",
        ProcessingStatus.Skipped => "Bỏ qua",
        _ => Status.ToString()
    };
    public string DetailMessage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                return ErrorMessage!;
            if (!string.IsNullOrWhiteSpace(Warning))
                return Warning!;
            if (Status == ProcessingStatus.Success)
                return $"OK {FileName} -> {OutputFileName} | {SavingsSummary}";
            if (Status == ProcessingStatus.Processing)
                return "Đang xử lý file này...";
            return "Sẵn sàng nén AVIF, file gốc sẽ không bị sửa.";
        }
    }
    public bool HasOutput => OutputSizeBytes.HasValue;
    public bool IsProcessing => Status == ProcessingStatus.Processing;
    public bool HasWarning => !string.IsNullOrWhiteSpace(Warning) || Status == ProcessingStatus.Warning;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage) || Status == ProcessingStatus.Error;

    public string? OutputPath
    {
        get => _outputPath;
        set { _outputPath = value; OnPropertyChanged(); }
    }

    public long? OutputSizeBytes
    {
        get => _outputSizeBytes;
        set
        {
            _outputSizeBytes = value;
            OnPropertyChanged();
            NotifyResultPropertiesChanged();
        }
    }

    public ProcessingStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(DetailMessage));
            OnPropertyChanged(nameof(IsProcessing));
            OnPropertyChanged(nameof(HasWarning));
            OnPropertyChanged(nameof(HasError));
        }
    }

    public string? Warning
    {
        get => _warning;
        set
        {
            _warning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailMessage));
            OnPropertyChanged(nameof(HasWarning));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailMessage));
            OnPropertyChanged(nameof(HasError));
        }
    }

    public int? AvifCrfUsed
    {
        get => _avifCrfUsed;
        set { _avifCrfUsed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CompressionDetail)); OnPropertyChanged(nameof(DetailMessage)); }
    }

    public int? MaxLongEdgeUsed
    {
        get => _maxLongEdgeUsed;
        set { _maxLongEdgeUsed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CompressionDetail)); OnPropertyChanged(nameof(DetailMessage)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void NotifyResultPropertiesChanged()
    {
        OnPropertyChanged(nameof(FormattedOutputSize));
        OnPropertyChanged(nameof(SavedBytes));
        OnPropertyChanged(nameof(SavedPercent));
        OnPropertyChanged(nameof(OutputFileName));
        OnPropertyChanged(nameof(OutputFolder));
        OnPropertyChanged(nameof(SizeComparison));
        OnPropertyChanged(nameof(SavingsSummary));
        OnPropertyChanged(nameof(DetailMessage));
        OnPropertyChanged(nameof(HasOutput));
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
