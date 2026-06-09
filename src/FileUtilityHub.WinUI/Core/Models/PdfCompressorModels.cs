using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Core.Models;

public partial class PdfPageItem : ObservableObject
{
    public int PageNumber { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _thumbnailPath = string.Empty;

    [ObservableProperty]
    private string _previewImagePath = string.Empty;

    [ObservableProperty]
    private ProcessingStatus _status = ProcessingStatus.Pending;

    [ObservableProperty]
    private string? _errorMessage;
    
    [ObservableProperty]
    private double _rotationAngle = 0;

    public double SavedRotationAngle { get; set; }

    public string DisplayPageNumber => $"Trang {PageNumber}";
}

public partial class PdfCompressionSettings : ObservableObject
{
    [ObservableProperty]
    private int _dpi = 200;

    [ObservableProperty]
    private int _jpegQuality = 80;

    [ObservableProperty]
    private PdfColorMode _colorMode = PdfColorMode.Rgb;
}

public class PdfCompressionVersion
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SettingsSummary { get; set; } = string.Empty;
    public List<int> CompressedPages { get; set; } = new();

    public string FormattedSize => $"{FileSizeBytes / 1048576.0:F2} MB";
    public string DisplayTime => CreatedAt.ToString("HH:mm:ss");
}

public class PdfDocumentSession
{
    public string OriginalPath { get; set; } = string.Empty;
    public string CurrentVersionPath { get; set; } = string.Empty;
    
    public List<PdfPageItem> Pages { get; set; } = new();
    public List<PdfCompressionVersion> VersionHistory { get; set; } = new();
}
