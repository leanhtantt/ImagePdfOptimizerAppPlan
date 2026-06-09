using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileUtilityHub_WinUI.Core.Contracts;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Core.Services;
using Windows.Storage;

namespace FileUtilityHub_WinUI.Features.ImageOptimizer;

public partial class ImageOptimizerViewModel : ObservableObject
{
    private readonly FileScanService _scanService;
    private readonly ImageConvertService _convertService;
    private readonly AppStatusService _statusService;
    private readonly IFeatureHandoffService _handoffService;
    private readonly IFilePickerService _filePickerService;
    private readonly INotificationService _notificationService;
    private CancellationTokenSource? _cancellationTokenSource;
    private List<ImageItem> _selectedItems = new();

    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".avif"];

    [ObservableProperty]
    private string _currentFolder = string.Empty;

    [ObservableProperty]
    private bool _isConverting = false;

    [ObservableProperty]
    private bool _hasImages = false;

    [ObservableProperty]
    private bool _hasAvifOutput = false;

    [ObservableProperty]
    private double _avifCrf = 24;

    [ObservableProperty]
    private int _resolutionIndex = 0;

    // Warning state — uses domain enum, not WinUI InfoBarSeverity
    [ObservableProperty]
    private string _warningTitle = string.Empty;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveWarning = false;

    [ObservableProperty]
    private WarningSeverityLevel _warningSeverity = WarningSeverityLevel.Warning;

    public ObservableCollection<ImageItem> ImageItems { get; } = new();

    public ImageOptimizerViewModel(
        FileScanService scanService,
        ImageConvertService convertService,
        AppStatusService statusService,
        IFeatureHandoffService handoffService,
        IFilePickerService filePickerService,
        INotificationService notificationService)
    {
        _scanService = scanService;
        _convertService = convertService;
        _statusService = statusService;
        _handoffService = handoffService;
        _filePickerService = filePickerService;
        _notificationService = notificationService;

        LoadSettings();
        PropertyChanged += ImageOptimizerViewModel_PropertyChanged;
    }

    private void ImageOptimizerViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AvifCrf) or nameof(ResolutionIndex))
        {
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["ImageOptimizer_AvifCrf"] is double crf) AvifCrf = crf;
            if (localSettings.Values["ImageOptimizer_ResolutionIndex"] is int resIndex) ResolutionIndex = resIndex;
        }
        catch { /* Ignore if unpackaged */ }
    }

    private void SaveSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["ImageOptimizer_AvifCrf"] = AvifCrf;
            localSettings.Values["ImageOptimizer_ResolutionIndex"] = ResolutionIndex;
        }
        catch { /* Ignore if unpackaged */ }
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var paths = await _filePickerService.PickFilesAsync(SupportedExtensions);
        foreach (var path in paths)
        {
            var fileInfo = new FileInfo(path);
            var ext = Path.GetExtension(path).TrimStart('.');
            ImageItems.Add(new ImageItem
            {
                FileName = fileInfo.Name,
                SourcePath = fileInfo.FullName,
                Format = ext,
                OriginalSizeBytes = fileInfo.Length
            });
        }
        UpdateStatus();
    }

    [RelayCommand]
    private async Task ChooseFolderAsync()
    {
        var folderPath = await _filePickerService.PickFolderAsync();
        if (folderPath != null)
        {
            CurrentFolder = folderPath;
            var result = _scanService.ScanDirectory(CurrentFolder);
            ImageItems.Clear();
            foreach (var item in result.ValidFiles)
            {
                ImageItems.Add(item);
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void RemoveSelected(object selectedItems)
    {
        if (selectedItems is System.Collections.Generic.IEnumerable<object> list)
        {
            var itemsToRemove = list.Cast<ImageItem>().ToList();
            foreach (var item in itemsToRemove)
            {
                ImageItems.Remove(item);
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        ImageItems.Clear();
        _selectedItems.Clear();
        HasActiveWarning = false;
        UpdateStatus();
    }

    public void UpdateSelection(System.Collections.Generic.IList<object> selectedItems)
    {
        _selectedItems = selectedItems.Cast<ImageItem>().ToList();
        HasSentToMerge = false;
        NotifyCommandsCanExecuteChanged();
    }

    [RelayCommand]
    private void OpenOutput()
    {
        if (!string.IsNullOrEmpty(CurrentFolder))
        {
            var outDir = Path.Combine(CurrentFolder, "compressed-avif");
            if (Directory.Exists(outDir))
            {
                System.Diagnostics.Process.Start("explorer.exe", outDir);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelConversion()
    {
        _cancellationTokenSource?.Cancel();
    }

    private bool CanCancel() => IsConverting;

    [RelayCommand(CanExecute = nameof(CanConvertAvif))]
    private async Task ConvertAvifAsync()
    {
        if (ImageItems.Count == 0) return;

        IsConverting = true;
        HasActiveWarning = false;
        NotifyCommandsCanExecuteChanged();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var itemsToConvert = _selectedItems.Count > 0 ? _selectedItems : ImageItems.ToList();
        _statusService.StartProcessing("Chuẩn bị nén...", itemsToConvert.Count);

        int res = ResolutionIndex switch
        {
            1 => 1920,
            2 => 2048,
            3 => 2560,
            _ => 0
        };

        var config = new ImageConvertConfig
        {
            AvifCrf = (int)AvifCrf,
            MaxLongEdge = res
        };

        if (string.IsNullOrEmpty(CurrentFolder))
        {
            CurrentFolder = Path.GetDirectoryName(itemsToConvert.First().SourcePath) ?? string.Empty;
        }

        // Reset state for multiple runs
        foreach (var item in itemsToConvert)
        {
            if (item.Status == ProcessingStatus.Processing) continue;
            item.Status = ProcessingStatus.Pending;
            item.OutputSizeBytes = null;
            item.OutputPath = null;
            item.ErrorMessage = null;
            item.Warning = null;
        }

        for (int i = 0; i < itemsToConvert.Count; i++)
        {
            if (token.IsCancellationRequested)
            {
                _statusService.StopProcessing("Đã dừng nén.");
                break;
            }

            var item = itemsToConvert[i];
            item.Status = ProcessingStatus.Processing;

            _statusService.ReportProgress(i, itemsToConvert.Count, $"Đang nén: {item.FileName}");

            await _convertService.ConvertToAvifAsync(item, config, CurrentFolder);

            _statusService.ReportProgress(i + 1, itemsToConvert.Count, $"Đang nén: {item.FileName}");
        }

        if (!token.IsCancellationRequested)
        {
            _statusService.StopProcessing("Đã hoàn tất nén AVIF toàn bộ ảnh trong danh sách!");
            _notificationService.ShowToast("Nén ảnh hoàn tất", $"Đã xử lý xong {ImageItems.Count} ảnh.", "ms-appx:///Assets/Square44x44Logo.scale-200.png");
        }

        // Show warning using domain severity, not WinUI InfoBarSeverity
        var warningItems = ImageItems.Where(i => i.Status == ProcessingStatus.Warning).ToList();
        if (warningItems.Count > 0)
        {
            HasActiveWarning = true;
            WarningTitle = "Một số file output nặng hơn gốc";
            WarningMessage = $"{warningItems.Count} file cần xem lại CRF hoặc giữ file gốc.";
            WarningSeverity = WarningSeverityLevel.Warning;
        }

        var errorItems = ImageItems.Where(i => i.Status == ProcessingStatus.Error).ToList();
        if (errorItems.Count > 0)
        {
            HasActiveWarning = true;
            WarningTitle = warningItems.Count > 0 ? "Có lỗi và cảnh báo" : "Có file nén thất bại";
            WarningMessage = $"{errorItems.Count} file lỗi. Kiểm tra FFmpeg hoặc file input.";
            WarningSeverity = WarningSeverityLevel.Error;
        }

        HasAvifOutput = ImageItems.Any(i => i.Status == ProcessingStatus.Success && i.OutputPath != null);

        IsConverting = false;
        NotifyCommandsCanExecuteChanged();
    }

    private bool CanConvertAvif() => !IsConverting && ImageItems.Count > 0;

    [ObservableProperty]
    private bool _hasSentToMerge = false;

    // Handoff commands (doc 09 section 4)
    [RelayCommand(CanExecute = nameof(CanSendToMerge))]
    private void SendToMerge(object parameter)
    {
        var context = BuildHandoffContext(parameter);
        _handoffService.NavigateToMerge(context);
        HasSentToMerge = true;
        NotifyCommandsCanExecuteChanged();
    }

    private bool CanSendToMerge() => HasImages && !IsConverting && !HasSentToMerge;

    [RelayCommand(CanExecute = nameof(CanMergeAndCompress))]
    private void MergeAndCompress(object parameter)
    {
        var context = BuildHandoffContext(parameter);
        _handoffService.NavigateToMergeAndCompress(context);
        HasSentToMerge = true;
        NotifyCommandsCanExecuteChanged();
    }

    private bool CanMergeAndCompress() => HasAvifOutput && !IsConverting && !HasSentToMerge;

    private FileBatchContext BuildHandoffContext(object? parameter)
    {
        var selectedItems = parameter as System.Collections.Generic.IList<object>;
        bool hasSelection = selectedItems != null && selectedItems.Count > 0;

        var sourceItems = hasSelection && selectedItems != null
            ? selectedItems.Cast<ImageItem>() 
            : ImageItems;

        return new FileBatchContext
        {
            SourceFeature = "ImageOptimizer",
            SourceFolder = CurrentFolder,
            Files = sourceItems
                .Select(i => i.OutputPath ?? i.SourcePath)
                .ToList(),
        };
    }

    // Drag & drop handler for DropZoneControl
    [RelayCommand]
    private void HandleDrop(object parameter)
    {
        if (parameter is IReadOnlyList<IStorageItem> items)
        {
            foreach (var storageItem in items)
            {
                if (storageItem is StorageFolder folder)
                {
                    CurrentFolder = folder.Path;
                    var result = _scanService.ScanDirectory(folder.Path);
                    foreach (var item in result.ValidFiles)
                    {
                        ImageItems.Add(item);
                    }
                }
                else if (storageItem is StorageFile file)
                {
                    var ext = Path.GetExtension(file.Path).ToLowerInvariant();
                    if (SupportedExtensions.Contains(ext))
                    {
                        var fileInfo = new FileInfo(file.Path);
                        ImageItems.Add(new ImageItem
                        {
                            FileName = fileInfo.Name,
                            SourcePath = fileInfo.FullName,
                            Format = ext.TrimStart('.'),
                            OriginalSizeBytes = fileInfo.Length
                        });
                    }
                }
            }
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        HasImages = ImageItems.Count > 0;
        HasAvifOutput = ImageItems.Any(i => i.Status == ProcessingStatus.Success && i.OutputPath != null);
        HasSentToMerge = false;
        _statusService.CurrentItemCount = ImageItems.Count;
        _statusService.StatusMessage = $"Đã tải {ImageItems.Count} ảnh vào danh sách chờ.";
        NotifyCommandsCanExecuteChanged();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        ConvertAvifCommand.NotifyCanExecuteChanged();
        CancelConversionCommand.NotifyCanExecuteChanged();
        SendToMergeCommand.NotifyCanExecuteChanged();
        MergeAndCompressCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConvertingChanged(bool value)
    {
        NotifyCommandsCanExecuteChanged();
    }
}
