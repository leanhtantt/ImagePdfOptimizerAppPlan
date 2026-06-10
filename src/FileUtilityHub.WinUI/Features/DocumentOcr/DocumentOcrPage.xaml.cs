using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Features.DocumentOcr;

public sealed partial class DocumentOcrPage : Page
{
    public DocumentOcrViewModel ViewModel { get; }

    public DocumentOcrPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<DocumentOcrViewModel>();
        this.DataContext = ViewModel;

        this.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPagePointerPressed), true);
        this.InitializeComponent();
        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void OnRemoveSelectedClick(object sender, RoutedEventArgs e)
    {
        var selected = FileList.SelectedItems.Cast<object>().ToList();
        ViewModel.RemoveSelectedCommand.Execute(selected);
    }

    private void OnOcrClick(object sender, RoutedEventArgs e)
    {
        var selected = FileList.SelectedItems.Cast<object>().ToList();
        ViewModel.OcrToWordCommand.Execute(selected);
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
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.DocumentOcrItem item)
        {
            OpenFile(item.SourcePath);
        }
    }

    private void MenuFlyoutItem_Open_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.DocumentOcrItem item)
        {
            OpenFile(item.SourcePath);
        }
    }

    private void MenuFlyoutItem_OpenFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.DocumentOcrItem item)
        {
            OpenFolderAndSelectFile(item.SourcePath);
        }
    }

    private void MenuFlyoutItem_Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.DocumentOcrItem item)
        {
            ViewModel.RemoveSelectedCommand.Execute(new System.Collections.Generic.List<object> { item });
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
