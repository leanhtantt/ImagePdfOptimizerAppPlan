using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FileUtilityHub_WinUI.Features.ImageOptimizer;

public sealed partial class ImageOptimizerPage : Page
{
    public ImageOptimizerViewModel ViewModel { get; }

    public ImageOptimizerPage()
    {
        this.InitializeComponent();
        
        // Resolve ViewModel from DI container
        ViewModel = App.Current.Services.GetRequiredService<ImageOptimizerViewModel>();
        this.DataContext = ViewModel;
    }

    private void OnRemoveSelectedClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FileList.SelectedItems.Count > 0)
        {
            // We pass a copy to avoid modified-while-iterating issues if the ViewModel alters the selection implicitly
            var selectedItems = new System.Collections.Generic.List<object>(FileList.SelectedItems);
            ViewModel.RemoveSelectedCommand.Execute(selectedItems);
        }
    }

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateSelection(FileList.SelectedItems);
    }

    private void FileList_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var clickedElement = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;
        var clickedItem = FindVisualParent<ListViewItem>(clickedElement);
        
        // If the user clicked on something that isn't a ListViewItem (like empty space), clear the selection
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
