using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FileUtilityHub_WinUI.Core.Contracts;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Core.Services;
using Windows.Storage;

namespace FileUtilityHub_WinUI.Features.PdfSplit;

public class PdfSplitViewModel : INotifyPropertyChanged
{
    private readonly AppStatusService _statusService;
    private readonly INotificationService _notificationService;
    private readonly IFilePickerService _filePickerService;
    private readonly PdfSplitService _splitService;

    private CancellationTokenSource? _cts;
    private bool _isProcessing;
    private bool _hasFiles;

    // Settings backing fields
    private int _dpiPresetIndex = 0;
    private int _customDpi = 150;
    private bool _showCustomDpi;
    private int _outputFormatIndex = 0;
    private int _jpegQuality = 90;
    private int _avifCrf = 28;
    private int _webPQuality = 85;

    // Warning
    private bool _hasActiveWarning;
    private string _warningTitle = "";
    private string _warningMessage = "";
    private int _warningSeverity;

    private static readonly string[] SupportedExtensions =
        { ".pdf", ".docx" };

    public ObservableCollection<PdfSplitItem> SplitItems { get; } = new();

    public bool HasFiles
    {
        get => _hasFiles;
        private set { _hasFiles = value; OnPropertyChanged(); }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set { _isProcessing = value; OnPropertyChanged(); }
    }

    // Settings properties
    public int DpiPresetIndex
    {
        get => _dpiPresetIndex;
        set
        {
            _dpiPresetIndex = value;
            ShowCustomDpi = value == 3;
            OnPropertyChanged();
        }
    }

    public int CustomDpi
    {
        get => _customDpi;
        set { _customDpi = Math.Clamp(value, 72, 600); OnPropertyChanged(); }
    }

    public bool ShowCustomDpi
    {
        get => _showCustomDpi;
        private set { _showCustomDpi = value; OnPropertyChanged(); }
    }

    public int OutputFormatIndex
    {
        get => _outputFormatIndex;
        set
        {
            _outputFormatIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowJpegQuality));
            OnPropertyChanged(nameof(ShowAvifCrf));
            OnPropertyChanged(nameof(ShowWebPQuality));
        }
    }

    public bool ShowJpegQuality => OutputFormatIndex == 0;
    public bool ShowAvifCrf => OutputFormatIndex == 2;
    public bool ShowWebPQuality => OutputFormatIndex == 3;

    public int JpegQuality
    {
        get => _jpegQuality;
        set { _jpegQuality = Math.Clamp(value, 1, 100); OnPropertyChanged(); }
    }

    public int AvifCrf
    {
        get => _avifCrf;
        set { _avifCrf = Math.Clamp(value, 0, 63); OnPropertyChanged(); }
    }

    public int WebPQuality
    {
        get => _webPQuality;
        set { _webPQuality = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
    }

    // Warning properties
    public bool HasActiveWarning
    {
        get => _hasActiveWarning;
        set { _hasActiveWarning = value; OnPropertyChanged(); }
    }

    public string WarningTitle
    {
        get => _warningTitle;
        set { _warningTitle = value; OnPropertyChanged(); }
    }

    public string WarningMessage
    {
        get => _warningMessage;
        set { _warningMessage = value; OnPropertyChanged(); }
    }

    public int WarningSeverity
    {
        get => _warningSeverity;
        set { _warningSeverity = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand AddFilesCommand { get; }
    public ICommand ChooseFolderCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand StartSplitCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand HandleDropCommand { get; }
    public ICommand OpenOutputCommand { get; }

    public PdfSplitViewModel(
        AppStatusService statusService,
        INotificationService notificationService,
        IFilePickerService filePickerService,
        PdfSplitService splitService)
    {
        _statusService = statusService;
        _notificationService = notificationService;
        _filePickerService = filePickerService;
        _splitService = splitService;

        AddFilesCommand = new RelayCommand(async () => await AddFilesAsync());
        ChooseFolderCommand = new RelayCommand(async () => await ChooseFolderAsync());
        ClearAllCommand = new RelayCommand(ClearAll);
        StartSplitCommand = new RelayCommand(async () => await StartSplitAsync());
        CancelCommand = new RelayCommand(Cancel);
        HandleDropCommand = new RelayCommand<object>(HandleDrop);
        OpenOutputCommand = new RelayCommand(OpenFirstOutput);
    }

    private async Task AddFilesAsync()
    {
        var files = await _filePickerService.PickFilesAsync(SupportedExtensions);
        AddFiles(files);
    }

    private async Task ChooseFolderAsync()
    {
        var folderPath = await _filePickerService.PickFolderAsync();
        if (string.IsNullOrEmpty(folderPath)) return;

        var files = ScanFolderForFiles(folderPath);
        AddFiles(files);
    }

    private void HandleDrop(object? parameter)
    {
        if (parameter is IReadOnlyList<IStorageItem> items)
        {
            foreach (var storageItem in items)
            {
                if (storageItem is StorageFolder folder)
                {
                    var files = ScanFolderForFiles(folder.Path);
                    AddFiles(files);
                }
                else if (storageItem is StorageFile file)
                {
                    var ext = Path.GetExtension(file.Path).ToLowerInvariant();
                    if (SupportedExtensions.Contains(ext))
                    {
                        AddFiles(new List<string> { file.Path });
                    }
                }
            }
        }
    }

    private static List<string> ScanFolderForFiles(string folderPath)
    {
        var results = new List<string>();
        foreach (var ext in SupportedExtensions)
        {
            results.AddRange(Directory.EnumerateFiles(folderPath, $"*{ext}", SearchOption.AllDirectories));
        }
        return results;
    }

    private void AddFiles(IReadOnlyList<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            if (SplitItems.Any(i => i.SourcePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                continue;

            var fi = new FileInfo(path);
            SplitItems.Add(new PdfSplitItem
            {
                FileName = fi.Name,
                SourcePath = fi.FullName,
                Format = fi.Extension.TrimStart('.').ToLowerInvariant(),
                OriginalSizeBytes = fi.Length
            });
        }

        HasFiles = SplitItems.Count > 0;
        _statusService.CurrentItemCount = SplitItems.Count;
    }

    public void RemoveItems(IList<object> selectedItems)
    {
        var toRemove = selectedItems.OfType<PdfSplitItem>().ToList();
        foreach (var item in toRemove)
            SplitItems.Remove(item);

        HasFiles = SplitItems.Count > 0;
        _statusService.CurrentItemCount = SplitItems.Count;
    }

    private void ClearAll()
    {
        SplitItems.Clear();
        HasFiles = false;
        _statusService.CurrentItemCount = 0;
        HasActiveWarning = false;
    }

    private async Task StartSplitAsync()
    {
        // Allow re-extract: process all items (Pending, Error, AND Success for re-run)
        var itemsToProcess = SplitItems.ToList();

        if (itemsToProcess.Count == 0) return;

        IsProcessing = true;
        _cts = new CancellationTokenSource();

        var config = new PdfSplitConfig
        {
            DpiPresetIndex = DpiPresetIndex,
            CustomDpi = CustomDpi,
            OutputFormatIndex = OutputFormatIndex,
            JpegQuality = JpegQuality,
            AvifCrf = AvifCrf,
            WebPQuality = WebPQuality
        };

        // Reset all items for re-processing
        foreach (var item in itemsToProcess)
        {
            item.Status = ProcessingStatus.Pending;
            item.ExtractedCount = 0;
            item.ErrorMessage = null;
            item.ElapsedTime = null;
        }

        _statusService.StartProcessing("Đang tách PDF thành ảnh...", itemsToProcess.Count);
        int successCount = 0, errorCount = 0, totalExtracted = 0;

        try
        {
            for (int i = 0; i < itemsToProcess.Count; i++)
            {
                var item = itemsToProcess[i];

                if (_cts.IsCancellationRequested)
                {
                    _statusService.StopProcessing("Đã dừng tách.");
                    break;
                }

                _statusService.ReportProgress(i, itemsToProcess.Count, $"Đang tách: {item.FileName}");

                var (_, extracted) = await _splitService.SplitAsync(item, config, _cts.Token);

                _statusService.ReportProgress(i + 1, itemsToProcess.Count, $"Xong: {item.FileName}");

                if (item.Status == ProcessingStatus.Success)
                {
                    successCount++;
                    totalExtracted += extracted;
                }
                else
                {
                    errorCount++;
                }
            }

            if (!_cts.IsCancellationRequested)
            {
                var summary = $"{successCount} thành công, {totalExtracted} ảnh";
                if (errorCount > 0) summary += $", {errorCount} lỗi";
                _statusService.StopProcessing($"Tách hoàn tất: {summary}");

                _notificationService.ShowToast(
                    "Tách PDF hoàn tất",
                    $"Đã tách {successCount} file → {totalExtracted} ảnh.",
                    "");
            }
        }
        catch (OperationCanceledException)
        {
            _statusService.StopProcessing("Đã hủy tách PDF.");
        }
        catch (Exception ex)
        {
            HasActiveWarning = true;
            WarningTitle = "Lỗi";
            WarningMessage = ex.Message;
            WarningSeverity = 2; // Error
            _statusService.StopProcessing("Lỗi khi tách PDF.");
        }
        finally
        {
            IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }

    private void OpenFirstOutput()
    {
        var firstSuccess = SplitItems.FirstOrDefault(i =>
            i.Status == ProcessingStatus.Success && !string.IsNullOrEmpty(i.OutputFolderPath));

        if (firstSuccess?.OutputFolderPath != null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = firstSuccess.OutputFolderPath,
                UseShellExecute = true
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Simple relay commands (same pattern used across all ViewModels)
internal class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}

internal class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    public RelayCommand(Action<T?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter is T t ? t : default);
}
