using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using FileUtilityHub_WinUI.Core.Services;
using FileUtilityHub.Core.Models;
using System.Collections.ObjectModel;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;
using FileUtilityHub_WinUI.Infrastructure.FileSystem;

namespace FileUtilityHub_WinUI.Features.ImagePdfOptimizer;

public sealed partial class ImagePdfOptimizerPage : Page
{
    private readonly FileScanService _scanService;
    private readonly ImageConvertService _convertService;
    private ObservableCollection<ImageItem> _imageItems = new();
    private string _currentFolder = string.Empty;

    public ImagePdfOptimizerPage()
    {
        this.InitializeComponent();
        _scanService = new FileScanService();
        _convertService = new ImageConvertService(new FfmpegRunner(new FfmpegLocator()), new OutputManager());
    }

    private async void BtnAddFiles_Click(object sender, RoutedEventArgs e)
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
            _imageItems.Add(new ImageItem
            {
                FileName = fileInfo.Name,
                SourcePath = fileInfo.FullName,
                Format = ext,
                OriginalSizeBytes = fileInfo.Length
            });
        }
        UpdateStatus();
    }

    private async void BtnChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        
        folderPicker.FileTypeFilter.Add("*");
        var folder = await folderPicker.PickSingleFolderAsync();
        
        if (folder != null)
        {
            _currentFolder = folder.Path;
            var result = _scanService.ScanDirectory(_currentFolder);
            _imageItems.Clear();
            foreach (var item in result.ValidFiles)
            {
                _imageItems.Add(item);
            }
            UpdateStatus();
        }
    }

    private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = FileList.SelectedItems.Cast<ImageItem>().ToList();
        foreach (var item in selectedItems)
        {
            _imageItems.Remove(item);
        }
        UpdateStatus();
    }

    private void BtnClearAll_Click(object sender, RoutedEventArgs e)
    {
        _imageItems.Clear();
        UpdateStatus();
    }

    private void BtnOpenOutput_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentFolder))
        {
            var outDir = Path.Combine(_currentFolder, "compressed-avif");
            if (Directory.Exists(outDir))
            {
                System.Diagnostics.Process.Start("explorer.exe", outDir);
            }
        }
    }

    private void UpdateStatus()
    {
        TxtStatusMessage.Text = $"Đã tải {_imageItems.Count} ảnh vào danh sách chờ.";
    }

    private async void BtnConvertAvif_Click(object sender, RoutedEventArgs e)
    {
        if (_imageItems.Count == 0) return;

        BtnConvertAvif.IsEnabled = false;
        ProgressAvif.Visibility = Visibility.Visible;
        ProgressAvif.Maximum = _imageItems.Count;
        ProgressAvif.Value = 0;

        int res = 0;
        if (ComboResolution.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
        {
            int.TryParse(cbi.Tag.ToString(), out res);
        }

        var config = new ImageConvertConfig
        {
            AvifCrf = (int)SliderCrf.Value,
            MaxLongEdge = res
        };

        if (string.IsNullOrEmpty(_currentFolder))
        {
            _currentFolder = Path.GetDirectoryName(_imageItems.First().SourcePath) ?? string.Empty;
        }

        for (int i = 0; i < _imageItems.Count; i++)
        {
            var item = _imageItems[i];
            TxtStatusMessage.Text = $"Đang nén {i + 1}/{_imageItems.Count}: {item.FileName}";
            
            await _convertService.ConvertToAvifAsync(item, config, _currentFolder);
            
            ProgressAvif.Value = i + 1;
        }

        TxtStatusMessage.Text = "Đã hoàn tất nén AVIF toàn bộ ảnh trong danh sách!";
        BtnConvertAvif.IsEnabled = true;
        ProgressAvif.Visibility = Visibility.Collapsed;
    }
}
