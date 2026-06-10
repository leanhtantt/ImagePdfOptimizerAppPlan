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

    private void RootGrid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var clickedElement = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;

        var clickedItem = FindVisualParent<ListViewItem>(clickedElement);
        if (clickedItem != null) return;

        var clickedButton = FindVisualParent<Microsoft.UI.Xaml.Controls.Primitives.ButtonBase>(clickedElement);
        if (clickedButton != null) return;

        var clickedInput = FindVisualParent<Microsoft.UI.Xaml.Controls.Control>(clickedElement);
        if (clickedInput is TextBox || clickedInput is NumberBox || clickedInput is ComboBox || clickedInput is ToggleSwitch || clickedInput is CommandBar) 
            return;

        FileList.SelectedItems.Clear();
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
