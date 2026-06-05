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
}
