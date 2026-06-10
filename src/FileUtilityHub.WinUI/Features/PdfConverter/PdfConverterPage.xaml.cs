using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FileUtilityHub_WinUI.Features.PdfConverter;

public sealed partial class PdfConverterPage : Page
{
    public PdfConverterViewModel ViewModel { get; }

    public PdfConverterPage()
    {
        this.InitializeComponent();

        // Resolve ViewModel from DI container
        ViewModel = App.Current.Services.GetRequiredService<PdfConverterViewModel>();
        this.DataContext = ViewModel;
    }

    private void OnRemoveSelectedClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FileList.SelectedItems.Count > 0)
        {
            var selectedItems = new List<object>(FileList.SelectedItems);
            ViewModel.RemoveSelectedCommand.Execute(selectedItems);
        }
    }

    private void OnConvertClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Pass selected items to the command; if nothing selected, pass null to convert all
        if (FileList.SelectedItems.Count > 0)
        {
            var selectedItems = new List<object>(FileList.SelectedItems);
            ViewModel.ConvertToPdfCommand.Execute(selectedItems);
        }
        else
        {
            ViewModel.ConvertToPdfCommand.Execute(null);
        }
    }

    private void OnOpenOutputFolderClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Open the output folder for the first selected item, or first item in list
        var selectedItem = FileList.SelectedItems.Count > 0
            ? FileList.SelectedItems[0] as Core.Models.PdfConvertItem
            : null;
        ViewModel.OpenOutputFolderCommand.Execute(selectedItem);
    }

    private void FileList_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var clickedElement = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;
        var clickedItem = FindVisualParent<ListViewItem>(clickedElement);

        // Click on empty space clears selection
        if (clickedItem == null)
        {
            FileList.SelectedItems.Clear();
        }
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
