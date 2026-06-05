using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileUtilityHub.Core.Contracts;
using FileUtilityHub.Core.Models;
using FileUtilityHub_WinUI.Core.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FileUtilityHub_WinUI.Features.ImageOptimizer;

public partial class ImageOptimizerViewModel : ObservableObject
{
    private readonly FileScanService _scanService;
    private readonly ImageConvertService _convertService;
    private readonly AppStatusService _statusService;
    private readonly IFeatureHandoffService _handoffService;
    private CancellationTokenSource? _cancellationTokenSource;

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

    // Warning state for InfoBar (doc 06 section 5.2)
    [ObservableProperty]
    private string _warningTitle = string.Empty;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveWarning = false;

    [ObservableProperty]
    private InfoBarSeverity _warningSeverity = InfoBarSeverity.Warning;

    public ObservableCollection<ImageItem> ImageItems { get; } = new();

    public ImageOptimizerViewModel(FileScanService scanService, ImageConvertService convertService, AppStatusService statusService, IFeatureHandoffService handoffService)
    {
        _scanService = scanService;
        _convertService = convertService;
        _statusService = statusService;
        _handoffService = handoffService;
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var picker = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
        
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".tif");
        picker.FileTypeFilter.Add(".tiff");
        picker.FileTypeFilter.Add(".avif");
        
        var files = await picker.PickMultipleFilesAsync();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.Path).TrimStart('.');
            var fileInfo = new FileInfo(file.Path);
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
        var folderPicker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        
        folderPicker.FileTypeFilter.Add("*");
        var folder = await folderPicker.PickSingleFolderAsync();
        
        if (folder != null)
        {
            CurrentFolder = folder.Path;
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
        if (selectedItems is System.Collections.IList list)
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
        HasActiveWarning = false;
        UpdateStatus();
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
        ConvertAvifCommand.NotifyCanExecuteChanged();
        CancelConversionCommand.NotifyCanExecuteChanged();
        SendToMergeCommand.NotifyCanExecuteChanged();
        MergeAndCompressCommand.NotifyCanExecuteChanged();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        _statusService.StartProcessing("Chuẩn bị nén...", ImageItems.Count);

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
            CurrentFolder = Path.GetDirectoryName(ImageItems.First().SourcePath) ?? string.Empty;
        }

        // Reset state for multiple runs
        foreach (var item in ImageItems)
        {
            if (item.Status == ProcessingStatus.Processing) continue;
            item.Status = ProcessingStatus.Pending;
            item.OutputSizeBytes = null;
            item.OutputPath = null;
            item.ErrorMessage = null;
            item.Warning = null;
        }

        for (int i = 0; i < ImageItems.Count; i++)
        {
            if (token.IsCancellationRequested)
            {
                _statusService.StopProcessing("Đã dừng nén.");
                break;
            }

            var item = ImageItems[i];
            item.Status = ProcessingStatus.Processing;
            
            _statusService.ReportProgress(i, ImageItems.Count, $"Đang nén: {item.FileName}");
            
            await _convertService.ConvertToAvifAsync(item, config, CurrentFolder);
            
            _statusService.ReportProgress(i + 1, ImageItems.Count, $"Đang nén: {item.FileName}");
        }

        if (!token.IsCancellationRequested)
        {
            _statusService.StopProcessing("Đã hoàn tất nén AVIF toàn bộ ảnh trong danh sách!");
        }

        // Show warning InfoBar if any items have warnings (doc 08 section 5)
        var warningItems = ImageItems.Where(i => i.Status == ProcessingStatus.Warning).ToList();
        if (warningItems.Count > 0)
        {
            HasActiveWarning = true;
            WarningTitle = "Một số file output nặng hơn gốc";
            WarningMessage = $"{warningItems.Count} file cần xem lại CRF hoặc giữ file gốc.";
            WarningSeverity = InfoBarSeverity.Warning;
        }

        var errorItems = ImageItems.Where(i => i.Status == ProcessingStatus.Error).ToList();
        if (errorItems.Count > 0)
        {
            HasActiveWarning = true;
            WarningTitle = warningItems.Count > 0 ? "Có lỗi và cảnh báo" : "Có file nén thất bại";
            WarningMessage = $"{errorItems.Count} file lỗi. Kiểm tra FFmpeg hoặc file input.";
            WarningSeverity = InfoBarSeverity.Error;
        }

        // Update HasAvifOutput for handoff buttons
        HasAvifOutput = ImageItems.Any(i => i.Status == ProcessingStatus.Success && i.OutputPath != null);

        IsConverting = false;
        ConvertAvifCommand.NotifyCanExecuteChanged();
        CancelConversionCommand.NotifyCanExecuteChanged();
        SendToMergeCommand.NotifyCanExecuteChanged();
        MergeAndCompressCommand.NotifyCanExecuteChanged();
    }

    private bool CanConvertAvif() => !IsConverting && ImageItems.Count > 0;

    // Handoff commands (doc 09 section 4)
    [RelayCommand(CanExecute = nameof(CanSendToMerge))]
    private void SendToMerge()
    {
        // Build FileBatchContext and navigate to File Merge / PDF Builder
        // Implementation will be wired in Phase 6 when FeatureHandoffService is ready
        var context = new FileBatchContext
        {
            SourceFeature = "ImageOptimizer",
            SourceFolder = CurrentFolder,
            Files = ImageItems
                .Where(i => i.OutputPath != null)
                .Select(i => i.OutputPath!)
                .ToList(),
        };
        _handoffService.NavigateToMerge(context);
    }

    private bool CanSendToMerge() => HasImages && !IsConverting;

    [RelayCommand(CanExecute = nameof(CanMergeAndCompress))]
    private void MergeAndCompress()
    {
        // Build FileBatchContext and run automation pipeline
        // Image Optimizer -> File Merge / PDF Builder -> PDF Compressor
        var context = new FileBatchContext
        {
            SourceFeature = "ImageOptimizer",
            SourceFolder = CurrentFolder,
            Files = ImageItems
                .Where(i => i.OutputPath != null)
                .Select(i => i.OutputPath!)
                .ToList(),
        };
        _handoffService.NavigateToMergeAndCompress(context);
    }

    private bool CanMergeAndCompress() => HasAvifOutput && !IsConverting;

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
                    var ext = Path.GetExtension(file.Path).ToLowerInvariant().TrimStart('.');
                    var supportedExts = new[] { "jpg", "jpeg", "png", "avif", "bmp", "tif", "tiff" };
                    if (supportedExts.Contains(ext))
                    {
                        var fileInfo = new FileInfo(file.Path);
                        ImageItems.Add(new ImageItem
                        {
                            FileName = fileInfo.Name,
                            SourcePath = fileInfo.FullName,
                            Format = ext,
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
        _statusService.CurrentItemCount = ImageItems.Count;
        _statusService.StatusMessage = $"Đã tải {ImageItems.Count} ảnh vào danh sách chờ.";
        ConvertAvifCommand.NotifyCanExecuteChanged();
        SendToMergeCommand.NotifyCanExecuteChanged();
        MergeAndCompressCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConvertingChanged(bool value)
    {
        ConvertAvifCommand.NotifyCanExecuteChanged();
        CancelConversionCommand.NotifyCanExecuteChanged();
        SendToMergeCommand.NotifyCanExecuteChanged();
        MergeAndCompressCommand.NotifyCanExecuteChanged();
    }
}
