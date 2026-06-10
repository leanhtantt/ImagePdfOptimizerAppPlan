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

        this.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPagePointerPressed), true);
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
