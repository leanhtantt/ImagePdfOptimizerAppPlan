using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileUtilityHub.Core.Models;
using FileUtilityHub_WinUI.Core.Services;
using Windows.Storage.Pickers;

namespace FileUtilityHub_WinUI.Features.ImagePdfOptimizer;

public partial class ImagePdfOptimizerViewModel : ObservableObject
{
    private readonly FileScanService _scanService;
    private readonly ImageConvertService _convertService;

    [ObservableProperty]
    private string _currentFolder = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng.";

    [ObservableProperty]
    private int _progressMaximum = 100;

    [ObservableProperty]
    private int _progressValue = 0;

    [ObservableProperty]
    private bool _isConverting = false;

    [ObservableProperty]
    private bool _hasImages = false;

    [ObservableProperty]
    private int _avifCrf = 24;

    [ObservableProperty]
    private int _resolutionIndex = 0;

    public ObservableCollection<ImageItem> ImageItems { get; } = new();

    public ImagePdfOptimizerViewModel(FileScanService scanService, ImageConvertService convertService)
    {
        _scanService = scanService;
        _convertService = convertService;
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

    [RelayCommand(CanExecute = nameof(CanConvertAvif))]
    private async Task ConvertAvifAsync()
    {
        if (ImageItems.Count == 0) return;

        IsConverting = true;
        ProgressMaximum = ImageItems.Count;
        ProgressValue = 0;

        int res = 0;
        switch (ResolutionIndex)
        {
            case 1: res = 1920; break;
            case 2: res = 2048; break;
            case 3: res = 2560; break;
            default: res = 0; break;
        }

        var config = new ImageConvertConfig
        {
            AvifCrf = AvifCrf,
            MaxLongEdge = res
        };

        if (string.IsNullOrEmpty(CurrentFolder))
        {
            CurrentFolder = Path.GetDirectoryName(ImageItems.First().SourcePath) ?? string.Empty;
        }

        for (int i = 0; i < ImageItems.Count; i++)
        {
            var item = ImageItems[i];
            StatusMessage = $"Đang nén {i + 1}/{ImageItems.Count}: {item.FileName}";
            
            await _convertService.ConvertToAvifAsync(item, config, CurrentFolder);
            
            ProgressValue = i + 1;
        }

        StatusMessage = "Đã hoàn tất nén AVIF toàn bộ ảnh trong danh sách!";
        IsConverting = false;
        ConvertAvifCommand.NotifyCanExecuteChanged();
    }

    private bool CanConvertAvif() => !IsConverting && ImageItems.Count > 0;

    private void UpdateStatus()
    {
        HasImages = ImageItems.Count > 0;
        StatusMessage = $"Đã tải {ImageItems.Count} ảnh vào danh sách chờ.";
        ConvertAvifCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConvertingChanged(bool value)
    {
        ConvertAvifCommand.NotifyCanExecuteChanged();
    }
}
