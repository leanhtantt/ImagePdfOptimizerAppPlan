using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileUtilityHub_WinUI.Core.Services;

public class AppStatusService : INotifyPropertyChanged
{
    private string _statusMessage = "Sẵn sàng.";
    private bool _isProcessing;
    private int _progressValue;
    private int _progressMaximum = 100;
    private string _progressPercentageText = "0%";
    private int _currentItemCount;
    private bool _isFfmpegMissing;

    public bool IsFfmpegMissing
    {
        get => _isFfmpegMissing;
        set { _isFfmpegMissing = value; OnPropertyChanged(); }
    }

    public int CurrentItemCount
    {
        get => _currentItemCount;
        set 
        { 
            _currentItemCount = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(ItemCountText)); 
        }
    }

    public string ItemCountText => $"Tổng: {CurrentItemCount} file";

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set { _isProcessing = value; OnPropertyChanged(); }
    }

    public int ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    public int ProgressMaximum
    {
        get => _progressMaximum;
        set { _progressMaximum = value; OnPropertyChanged(); }
    }

    public string ProgressPercentageText
    {
        get => _progressPercentageText;
        set { _progressPercentageText = value; OnPropertyChanged(); }
    }

    public void ReportProgress(int value, int maximum, string message = "")
    {
        ProgressMaximum = maximum;
        ProgressValue = value;
        ProgressPercentageText = $"{(int)(value * 100.0 / maximum)}%";
        
        if (!string.IsNullOrEmpty(message))
        {
            StatusMessage = message;
        }
    }

    public void StartProcessing(string message, int maximum = 100)
    {
        IsProcessing = true;
        ProgressValue = 0;
        ProgressMaximum = maximum;
        ProgressPercentageText = "0%";
        StatusMessage = message;
    }

    public void StopProcessing(string message = "Hoàn tất.")
    {
        IsProcessing = false;
        StatusMessage = message;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
