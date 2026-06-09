using System;
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

namespace FileUtilityHub_WinUI.Features.PdfCompressor;

public partial class PdfCompressorViewModel : ObservableObject
{
    private readonly PdfCompressorService _compressorService;
    private readonly IFilePickerService _filePickerService;
    public AppStatusService StatusService { get; }
    private readonly INotificationService _notificationService;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private PdfDocumentSession? _session;

    [ObservableProperty]
    private PdfCompressionSettings _settings = new();

    [ObservableProperty]
    private string _pageRangeInput = string.Empty;

    [ObservableProperty]
    private PdfPageItem? _selectedPreviewPage;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _hasFile;

    public ObservableCollection<PdfPageItem> Pages { get; } = new();
    public ObservableCollection<PdfCompressionVersion> VersionHistory { get; } = new();

    public PdfCompressorViewModel(
        PdfCompressorService compressorService,
        IFilePickerService filePickerService,
        AppStatusService statusService,
        INotificationService notificationService)
    {
        _compressorService = compressorService;
        _filePickerService = filePickerService;
        StatusService = statusService;
        _notificationService = notificationService;

        LoadSettings();
        Settings.PropertyChanged += (s, e) => SaveSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["PdfCompressor_Dpi"] is int dpi) Settings.Dpi = dpi;
            if (localSettings.Values["PdfCompressor_JpegQuality"] is int quality) Settings.JpegQuality = quality;
        }
        catch { /* Ignore if unpackaged */ }
    }

    private void SaveSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["PdfCompressor_Dpi"] = Settings.Dpi;
            localSettings.Values["PdfCompressor_JpegQuality"] = Settings.JpegQuality;
        }
        catch { /* Ignore if unpackaged */ }
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var paths = await _filePickerService.PickFilesAsync(new[] { ".pdf" });
        if (paths.Count > 0)
        {
            await LoadPdfAsync(paths.First(), isReload: false);
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        if (Session == null || string.IsNullOrEmpty(Session.CurrentVersionPath)) return;
        string? dir = Path.GetDirectoryName(Session.CurrentVersionPath);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(dir);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var page in Pages) page.IsSelected = true;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var page in Pages) page.IsSelected = false;
    }

    private async Task LoadPdfAsync(string filePath, bool isReload)
    {
        IsProcessing = true;
        HasFile = false;

        var existingHistory = isReload && Session != null ? Session.VersionHistory.ToList() : null;

        Pages.Clear();
        VersionHistory.Clear();
        SelectedPreviewPage = null;

        StatusService.StartProcessing("Đang tải file PDF và tạo thumbnail...", 1);

        try
        {
            string tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PdfCompressorThumbs");
            Directory.CreateDirectory(tempFolder);

            Session = await _compressorService.LoadPdfAsync(filePath, tempFolder);
            
            foreach (var page in Session.Pages)
            {
                Pages.Add(page);
            }

            if (isReload && existingHistory != null)
            {
                Session.VersionHistory = existingHistory;
                foreach (var v in existingHistory) VersionHistory.Add(v);
            }
            else
            {
                // Initial version tracking
                var fileInfo = new FileInfo(filePath);
                var initVersion = new PdfCompressionVersion
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSizeBytes = fileInfo.Length,
                    CreatedAt = DateTime.Now,
                    SettingsSummary = "Bản gốc",
                };
                VersionHistory.Add(initVersion);
                Session.VersionHistory.Add(initVersion);
            }

            HasFile = true;
            if (Pages.Count > 0)
                SelectedPreviewPage = Pages[0];

            StatusService.StopProcessing("Tải PDF thành công.");
        }
        catch (Exception ex)
        {
            StatusService.StopProcessing("Lỗi khi tải PDF.");
            _notificationService.ShowToast("Lỗi", ex.Message, "");
        }
        finally
        {
            IsProcessing = false;
            CompressCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void ApplyPageRange()
    {
        if (string.IsNullOrWhiteSpace(PageRangeInput))
        {
            foreach (var page in Pages) page.IsSelected = false;
            return;
        }

        var parts = PageRangeInput.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var selectedNumbers = new System.Collections.Generic.HashSet<int>();

        foreach (var part in parts)
        {
            var p = part.Trim();
            if (p.Contains("-"))
            {
                var bounds = p.Split('-');
                if (bounds.Length == 2 && int.TryParse(bounds[0], out int start) && int.TryParse(bounds[1], out int end))
                {
                    for (int i = start; i <= end; i++) selectedNumbers.Add(i);
                }
            }
            else if (int.TryParse(p, out int num))
            {
                selectedNumbers.Add(num);
            }
        }

        foreach (var page in Pages)
        {
            page.IsSelected = selectedNumbers.Contains(page.PageNumber);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCompress))]
    private async Task CompressAsync()
    {
        if (Session == null) return;

        IsProcessing = true;
        CompressCommand.NotifyCanExecuteChanged();
        
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        StatusService.StartProcessing("Đang nén PDF...", Pages.Count);

        try
        {
            string outputDir = Path.GetDirectoryName(Session.OriginalPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            
            var version = await _compressorService.CompressPdfAsync(Session, Settings, outputDir, (current, total, msg) =>
            {
                StatusService.ReportProgress(current, total, msg);
            }, token);

            Session.CurrentVersionPath = version.FilePath;
            Session.VersionHistory.Add(version);
            VersionHistory.Add(version);

            _notificationService.ShowToast("Nén thành công", $"Đã nén {version.CompressedPages.Count} trang. Dung lượng mới: {version.FormattedSize}", "");

            // Reload thumbnails from the new version, but keep history
            await LoadPdfAsync(version.FilePath, isReload: true);
        }
        catch (OperationCanceledException)
        {
            StatusService.StopProcessing("Đã huỷ nén.");
        }
        catch (Exception ex)
        {
            StatusService.StopProcessing("Lỗi khi nén PDF.");
            _notificationService.ShowToast("Lỗi", ex.Message, "");
        }
        finally
        {
            IsProcessing = false;
            CompressCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanCompress() => HasFile && !IsProcessing;

    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanEditPdf))]
    private void RotateLeft()
    {
        var targets = Pages.Where(p => p.IsSelected).ToList();
        if (targets.Count == 0 && SelectedPreviewPage != null) targets.Add(SelectedPreviewPage);
        foreach (var page in targets) page.RotationAngle = NormalizeRotation(page.RotationAngle - 90);
    }

    [RelayCommand(CanExecute = nameof(CanEditPdf))]
    private void RotateRight()
    {
        var targets = Pages.Where(p => p.IsSelected).ToList();
        if (targets.Count == 0 && SelectedPreviewPage != null) targets.Add(SelectedPreviewPage);
        foreach (var page in targets) page.RotationAngle = NormalizeRotation(page.RotationAngle + 90);
    }

    [RelayCommand(CanExecute = nameof(CanEditPdf))]
    private async Task SavePdfAsync()
    {
        if (Session == null || !HasFile) return;

        bool hasChanges = Pages.Any(p => NormalizeRotation(p.RotationAngle - p.SavedRotationAngle) != 0);
        if (!hasChanges)
        {
            _notificationService.ShowToast("Không có thay đổi", "Chưa có trang nào được xoay để lưu.", "");
            return;
        }

        IsProcessing = true;
        StatusService.StartProcessing("Đang lưu file...", 1);

        try
        {
            string sourcePath = Session.CurrentVersionPath;
            string tempPath = CreateTemporarySavePath(sourcePath);
            int[] rotations = Pages
                .Select(page => (int)NormalizeRotation(page.RotationAngle - page.SavedRotationAngle))
                .ToArray();

            try
            {
                await Task.Run(() =>
                {
                    using var doc = PdfSharp.Pdf.IO.PdfReader.Open(sourcePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
                    if (doc.PageCount != rotations.Length)
                    {
                        throw new InvalidOperationException("Số trang PDF đã thay đổi. Vui lòng mở lại file rồi thử lại.");
                    }

                    for (int i = 0; i < doc.PageCount; i++)
                    {
                        if (rotations[i] != 0)
                        {
                            doc.Pages[i].Rotate = (int)NormalizeRotation(doc.Pages[i].Rotate + rotations[i]);
                        }
                    }
                    doc.Save(tempPath);
                });

                File.Move(tempPath, sourcePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }

            foreach (var page in Pages)
            {
                page.SavedRotationAngle = page.RotationAngle;
            }

            _notificationService.ShowToast("Thành công", "Đã lưu góc xoay vào file hiện tại.", "");
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast("Lỗi", "Không thể lưu PDF: " + ex.Message, "");
        }
        finally
        {
            StatusService.StopProcessing("Lưu hoàn tất.");
            IsProcessing = false;
        }
    }

    private bool CanEditPdf() => HasFile && !IsProcessing;

    private static double NormalizeRotation(double angle) => (angle % 360 + 360) % 360;

    private static string CreateTemporarySavePath(string sourcePath)
    {
        string dir = Path.GetDirectoryName(sourcePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string name = Path.GetFileNameWithoutExtension(sourcePath);
        return Path.Combine(dir, $".{name}_{Guid.NewGuid():N}.tmp.pdf");
    }

    partial void OnIsProcessingChanged(bool value)
    {
        CompressCommand.NotifyCanExecuteChanged();
        RotateLeftCommand.NotifyCanExecuteChanged();
        RotateRightCommand.NotifyCanExecuteChanged();
        SavePdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasFileChanged(bool value)
    {
        CompressCommand.NotifyCanExecuteChanged();
        RotateLeftCommand.NotifyCanExecuteChanged();
        RotateRightCommand.NotifyCanExecuteChanged();
        SavePdfCommand.NotifyCanExecuteChanged();
    }
}
