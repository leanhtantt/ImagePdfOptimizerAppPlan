using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Shared.Controls;

/// <summary>
/// Shared status badge control for file processing status.
/// Shows icon + text matching ProcessingStatus enum.
/// Reusable across all feature file lists.
/// StatusLabel property allows each feature to customize the text.
/// </summary>
public sealed partial class FileStatusBadge : UserControl
{
    public FileStatusBadge()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(ProcessingStatus), typeof(FileStatusBadge),
            new PropertyMetadata(ProcessingStatus.Pending, OnStatusChanged));

    /// <summary>
    /// Optional custom label override. If set, this text is displayed instead of the default.
    /// </summary>
    public static readonly DependencyProperty StatusLabelProperty =
        DependencyProperty.Register(nameof(StatusLabel), typeof(string), typeof(FileStatusBadge),
            new PropertyMetadata(null, OnStatusChanged));

    public ProcessingStatus Status
    {
        get => (ProcessingStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string? StatusLabel
    {
        get => (string?)GetValue(StatusLabelProperty);
        set => SetValue(StatusLabelProperty, value);
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileStatusBadge badge)
            badge.UpdateVisual();
    }

    private void UpdateVisual()
    {
        ProcessingRing.IsActive = false;
        ProcessingRing.Visibility = Visibility.Collapsed;
        StatusIcon.Visibility = Visibility.Visible;

        switch (Status)
        {
            case ProcessingStatus.Pending:
                StatusIcon.Glyph = "\uE823"; // Clock
                StatusText.Text = StatusLabel ?? "Chờ xử lý";
                break;
            case ProcessingStatus.Processing:
                StatusIcon.Visibility = Visibility.Collapsed;
                ProcessingRing.IsActive = true;
                ProcessingRing.Visibility = Visibility.Visible;
                StatusText.Text = StatusLabel ?? "Đang xử lý";
                break;
            case ProcessingStatus.Success:
                StatusIcon.Glyph = "\uE73E"; // Checkmark
                StatusText.Text = StatusLabel ?? "Hoàn tất";
                break;
            case ProcessingStatus.Warning:
                StatusIcon.Glyph = "\uE7BA"; // Warning
                StatusText.Text = StatusLabel ?? "Cần xem lại";
                break;
            case ProcessingStatus.Error:
                StatusIcon.Glyph = "\uEA39"; // Error
                StatusText.Text = StatusLabel ?? "Lỗi";
                break;
            case ProcessingStatus.Skipped:
                StatusIcon.Glyph = "\uE711"; // Skip
                StatusText.Text = StatusLabel ?? "Bỏ qua";
                break;
        }
    }
}
