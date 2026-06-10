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

namespace FileUtilityHub_WinUI.Features.DocumentOcr;

public partial class DocumentOcrViewModel : ObservableObject
{
    private readonly DocumentOcrWorkflowService _workflowService;
    private readonly OcrApiClient _apiClient;
    private readonly AppStatusService _statusService;
    private readonly IFilePickerService _filePickerService;
    private readonly INotificationService _notificationService;
    private CancellationTokenSource? _cancellationTokenSource;

    private static readonly string[] SupportedExtensions =
        [".pdf", ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp"];

    // --- Observable Properties ---

    [ObservableProperty]
    private bool _isProcessing = false;

    [ObservableProperty]
    private bool _hasFiles = false;

    [ObservableProperty]
    private OcrServerStatus _serverStatus = OcrServerStatus.Unknown;

    [ObservableProperty]
    private bool _isCheckingServer = false;

    // Warning state
    [ObservableProperty]
    private string _warningTitle = string.Empty;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveWarning = false;

    [ObservableProperty]
    private WarningSeverityLevel _warningSeverity = WarningSeverityLevel.Warning;

    public ObservableCollection<DocumentOcrItem> OcrItems { get; } = new();

    public string ServerStatusText => ServerStatus switch
    {
        OcrServerStatus.Unknown => "Chưa kiểm tra",
        OcrServerStatus.Checking => "Đang kiểm tra...",
        OcrServerStatus.Ready => "Sẵn sàng",
        OcrServerStatus.Unreachable => "Không kết nối được",
        OcrServerStatus.Busy => "Server đang bận",
        _ => "—"
    };

    public string ServerStatusIcon => ServerStatus switch
    {
        OcrServerStatus.Ready => "\uE73E",       // Checkmark
        OcrServerStatus.Unreachable => "\uE711",  // Cancel
        OcrServerStatus.Busy => "\uE823",         // Clock
        OcrServerStatus.Checking => "\uE895",     // Sync
        _ => "\uE9CE"                             // Unknown
    };

    public bool IsServerReady => ServerStatus == OcrServerStatus.Ready;

    public DocumentOcrViewModel(
        DocumentOcrWorkflowService workflowService,
        OcrApiClient apiClient,
        AppStatusService statusService,
        IFilePickerService filePickerService,
        INotificationService notificationService)
    {
        _workflowService = workflowService;
        _apiClient = apiClient;
        _statusService = statusService;
        _filePickerService = filePickerService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Called when the page is loaded. Auto-check server health.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckServerAsync();
    }

    partial void OnServerStatusChanged(OcrServerStatus value)
    {
        OnPropertyChanged(nameof(ServerStatusText));
        OnPropertyChanged(nameof(ServerStatusIcon));
        OnPropertyChanged(nameof(IsServerReady));
        OcrToWordCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var paths = await _filePickerService.PickFilesAsync(SupportedExtensions);
        foreach (var path in paths)
        {
            AddFileIfSupported(path);
        }
        UpdateStatus();
    }

    [RelayCommand]
    private async Task ChooseFolderAsync()
    {
        var folderPath = await _filePickerService.PickFolderAsync();
        if (folderPath != null)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files.OrderBy(f => f))
            {
                AddFileIfSupported(file);
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void RemoveSelected(object selectedItems)
    {
        if (selectedItems is IEnumerable<object> list)
        {
            var itemsToRemove = list.Cast<DocumentOcrItem>().ToList();
            foreach (var item in itemsToRemove)
            {
                OcrItems.Remove(item);
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        OcrItems.Clear();
        HasActiveWarning = false;
        UpdateStatus();
    }

    [RelayCommand]
    private async Task CheckServerAsync()
    {
        IsCheckingServer = true;
        ServerStatus = OcrServerStatus.Checking;

        var config = new DocumentOcrConfig();
        ServerStatus = await _apiClient.CheckHealthAsync(config.OcrServerUrl);

        IsCheckingServer = false;

        if (ServerStatus == OcrServerStatus.Unreachable)
        {
            HasActiveWarning = true;
            WarningTitle = "Không kết nối được OCR server";
            WarningMessage = $"Không thể kết nối đến {config.OcrServerUrl}. Vui lòng kiểm tra server đã chạy và mạng đã kết nối.";
            WarningSeverity = WarningSeverityLevel.Error;
        }
        else if (ServerStatus == OcrServerStatus.Busy)
        {
            HasActiveWarning = true;
            WarningTitle = "OCR server đang bận";
            WarningMessage = "Server đang xử lý tác vụ khác. Vui lòng thử lại sau.";
            WarningSeverity = WarningSeverityLevel.Warning;
        }
        else
        {
            HasActiveWarning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelOcr()
    {
        _cancellationTokenSource?.Cancel();
    }

    private bool CanCancel() => IsProcessing;

    [RelayCommand(CanExecute = nameof(CanOcr))]
    private async Task OcrToWordAsync(object? selectedItemsParam)
    {
        if (OcrItems.Count == 0) return;

        // Determine which items to process
        List<DocumentOcrItem> itemsToProcess;

        if (selectedItemsParam is IEnumerable<object> selectedList)
        {
            var selected = selectedList.Cast<DocumentOcrItem>().ToList();
            itemsToProcess = selected.Count > 0 ? selected : OcrItems.ToList();
        }
        else
        {
            itemsToProcess = OcrItems.ToList();
        }

        IsProcessing = true;
        HasActiveWarning = false;
        NotifyCommandsCanExecuteChanged();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var config = new DocumentOcrConfig();

        _statusService.StartProcessing("Đang OCR tài liệu...", itemsToProcess.Count);

        int successCount = 0;
        int errorCount = 0;

        try
        {
            for (int i = 0; i < itemsToProcess.Count; i++)
            {
                if (token.IsCancellationRequested)
                {
                    _statusService.StopProcessing("Đã dừng OCR.");
                    break;
                }

                var item = itemsToProcess[i];
                _statusService.ReportProgress(i, itemsToProcess.Count, $"Đang xử lý: {item.FileName}");

                var success = await _workflowService.ProcessFileAsync(item, config, token);

                if (success)
                    successCount++;
                else if (item.Status == OcrProcessingStatus.Error)
                    errorCount++;

                _statusService.ReportProgress(i + 1, itemsToProcess.Count, $"Xong: {item.FileName}");
            }

            if (!token.IsCancellationRequested)
            {
                var summary = $"{successCount} thành công";
                if (errorCount > 0) summary += $", {errorCount} lỗi";

                _statusService.StopProcessing($"OCR hoàn tất: {summary}");

                _notificationService.ShowToast(
                    "OCR tài liệu hoàn tất",
                    $"{summary} / {itemsToProcess.Count} file.",
                    "ms-appx:///Assets/Square44x44Logo.scale-200.png");

                if (errorCount > 0)
                {
                    HasActiveWarning = true;
                    WarningTitle = "Một số file bị lỗi";
                    WarningMessage = $"{errorCount} file không thể OCR. {successCount}/{itemsToProcess.Count} file đã hoàn tất.";
                    WarningSeverity = WarningSeverityLevel.Warning;
                }
            }
        }
        catch (Exception ex)
        {
            HasActiveWarning = true;
            WarningTitle = "Lỗi khi OCR tài liệu";
            WarningMessage = ex.Message;
            WarningSeverity = WarningSeverityLevel.Error;
            _statusService.StopProcessing("Lỗi khi OCR.");
        }
        finally
        {
            IsProcessing = false;
            NotifyCommandsCanExecuteChanged();
        }
    }

    private bool CanOcr() => !IsProcessing && OcrItems.Count > 0 && IsServerReady;

    [RelayCommand]
    private void HandleDrop(object parameter)
    {
        if (parameter is IReadOnlyList<IStorageItem> items)
        {
            foreach (var storageItem in items)
            {
                if (storageItem is StorageFolder folder)
                {
                    var files = Directory.GetFiles(folder.Path);
                    foreach (var file in files.OrderBy(f => f))
                    {
                        AddFileIfSupported(file);
                    }
                }
                else if (storageItem is StorageFile file)
                {
                    AddFileIfSupported(file.Path);
                }
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void OpenOutputFolder(DocumentOcrItem? item)
    {
        string? dir = null;

        if (item != null && !string.IsNullOrEmpty(item.OutputPath))
        {
            dir = Path.GetDirectoryName(item.OutputPath);
        }
        else if (OcrItems.Count > 0)
        {
            dir = Path.GetDirectoryName(OcrItems.First().SourcePath);
        }

        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }
    }

    private void AddFileIfSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(ext)) return;

        // Avoid duplicate paths
        if (OcrItems.Any(i => i.SourcePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            return;

        var fi = new FileInfo(filePath);
        OcrItems.Add(new DocumentOcrItem
        {
            FileName = fi.Name,
            SourcePath = fi.FullName,
            Format = ext.TrimStart('.'),
            OriginalSizeBytes = fi.Length
        });
    }

    private void UpdateStatus()
    {
        HasFiles = OcrItems.Count > 0;
        _statusService.CurrentItemCount = OcrItems.Count;
        _statusService.StatusMessage = $"Đã tải {OcrItems.Count} file vào danh sách OCR.";
        NotifyCommandsCanExecuteChanged();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        OcrToWordCommand.NotifyCanExecuteChanged();
        CancelOcrCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsProcessingChanged(bool value)
    {
        NotifyCommandsCanExecuteChanged();
    }
}
