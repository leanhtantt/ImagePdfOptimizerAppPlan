using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FileUtilityHub_WinUI.Features.FileMerger;

public sealed partial class FileMergerPage : Page
{
    public FileMergerViewModel ViewModel { get; }

    public FileMergerPage()
    {
        this.InitializeComponent();

        // Resolve ViewModel from DI container
        ViewModel = App.Current.Services.GetRequiredService<FileMergerViewModel>();
        this.DataContext = ViewModel;

        // Check for handoff from Image Optimizer
        this.Loaded += (_, _) => ViewModel.CheckForHandoff();
    }

    private void OnRemoveSelectedClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FileList.SelectedItems.Count > 0)
        {
            var selectedItems = new System.Collections.Generic.List<object>(FileList.SelectedItems);
            ViewModel.RemoveSelectedCommand.Execute(selectedItems);
        }
    }

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selection tracking (for potential future "merge selected only" feature)
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

    private void FileList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        // After reorder, update OrderIndex to reflect new positions
        for (int i = 0; i < ViewModel.MergeItems.Count; i++)
        {
            ViewModel.MergeItems[i].OrderIndex = i + 1;
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
