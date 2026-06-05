using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Shared.Controls;

/// <summary>
/// Shared status badge control for file processing status.
/// Shows icon + text matching ProcessingStatus enum.
/// Reusable across all feature file lists.
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

    public ProcessingStatus Status
    {
        get => (ProcessingStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
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
                StatusText.Text = "Chờ nén";
                break;
            case ProcessingStatus.Processing:
                StatusIcon.Visibility = Visibility.Collapsed;
                ProcessingRing.IsActive = true;
                ProcessingRing.Visibility = Visibility.Visible;
                StatusText.Text = "Đang xử lý";
                break;
            case ProcessingStatus.Success:
                StatusIcon.Glyph = "\uE73E"; // Checkmark
                StatusText.Text = "Đã nén";
                break;
            case ProcessingStatus.Warning:
                StatusIcon.Glyph = "\uE7BA"; // Warning
                StatusText.Text = "Cần xem lại";
                break;
            case ProcessingStatus.Error:
                StatusIcon.Glyph = "\uEA39"; // Error
                StatusText.Text = "Lỗi";
                break;
            case ProcessingStatus.Skipped:
                StatusIcon.Glyph = "\uE711"; // Skip
                StatusText.Text = "Bỏ qua";
                break;
        }
    }
}
