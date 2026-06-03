using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FileUtilityHub_WinUI.Features.ImagePdfOptimizer;

public sealed partial class ImagePdfOptimizerPage : Page
{
    public ImagePdfOptimizerViewModel ViewModel { get; }

    public ImagePdfOptimizerPage()
    {
        this.InitializeComponent();
        
        // Resolve ViewModel from DI container
        ViewModel = App.Current.Services.GetRequiredService<ImagePdfOptimizerViewModel>();
        this.DataContext = ViewModel;
    }
}
