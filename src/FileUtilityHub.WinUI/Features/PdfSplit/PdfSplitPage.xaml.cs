using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FileUtilityHub_WinUI.Features.PdfSplit;

/// <summary>
/// Code-behind for PdfSplitPage.
/// Resolves ViewModel from DI and wires up selection-based commands.
/// </summary>
public sealed partial class PdfSplitPage : Page
{
    public PdfSplitViewModel ViewModel { get; }

    public PdfSplitPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<PdfSplitViewModel>();
        this.InitializeComponent();
        this.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPagePointerPressed), true);
    }

    private void OnRemoveSelectedClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FileList.SelectedItems.Count > 0)
        {
            ViewModel.RemoveItems(FileList.SelectedItems);
        }
    }

    private void OnStartSplitClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FileList.SelectedItems.Count > 0)
        {
            var selectedItems = new System.Collections.Generic.List<object>(FileList.SelectedItems);
            ViewModel.StartSplitCommand.Execute(selectedItems);
        }
        else
        {
            ViewModel.StartSplitCommand.Execute(null);
        }
    }

    private void OnSelectBySizeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        double minMb = MinSizeNumberBox.Value;
        if (double.IsNaN(minMb)) return;

        double minBytes = minMb * 1048576.0; // 1 MB = 1048576 bytes

        FileList.SelectedItems.Clear();
        foreach (var item in ViewModel.SplitItems)
        {
            if (item.OriginalSizeBytes >= minBytes)
            {
                FileList.SelectedItems.Add(item);
            }
        }
    }

    private void OnPagePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var clickedElement = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;

        var clickedItem = FindVisualParent<Microsoft.UI.Xaml.Controls.ListViewItem>(clickedElement);
        if (clickedItem != null) return;

        var clickedButton = FindVisualParent<Microsoft.UI.Xaml.Controls.Primitives.ButtonBase>(clickedElement);
        if (clickedButton != null) return;

        var clickedInput = FindVisualParent<Microsoft.UI.Xaml.Controls.Control>(clickedElement);
        if (clickedInput is Microsoft.UI.Xaml.Controls.TextBox || 
            clickedInput is Microsoft.UI.Xaml.Controls.NumberBox || 
            clickedInput is Microsoft.UI.Xaml.Controls.ComboBox || 
            clickedInput is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton || 
            clickedInput is Microsoft.UI.Xaml.Controls.ToggleSwitch || 
            clickedInput is Microsoft.UI.Xaml.Controls.CommandBar ||
            clickedInput is Microsoft.UI.Xaml.Controls.Slider) 
            return;

        FileList.SelectedItems.Clear();
    }

    private void FileListItem_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.PdfSplitItem item)
        {
            OpenFile(item.SourcePath);
        }
    }

    private void MenuFlyoutItem_Open_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.PdfSplitItem item)
        {
            OpenFile(item.SourcePath);
        }
    }

    private void MenuFlyoutItem_OpenFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.PdfSplitItem item)
        {
            OpenFolderAndSelectFile(item.SourcePath);
        }
    }

    private void MenuFlyoutItem_Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.PdfSplitItem item)
        {
            ViewModel.RemoveItems(new System.Collections.Generic.List<object> { item });
        }
    }

    private void OpenFile(string filePath)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = filePath, UseShellExecute = true }); }
        catch { }
    }

    private void OpenFolderAndSelectFile(string filePath)
    {
        try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\""); }
        catch { }
    }

    private static T? FindVisualParent<T>(Microsoft.UI.Xaml.DependencyObject? child) where T : Microsoft.UI.Xaml.DependencyObject
    {
        while (child != null)
        {
            if (child is T parent)
                return parent;
            child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
