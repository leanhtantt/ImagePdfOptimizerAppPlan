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
}
