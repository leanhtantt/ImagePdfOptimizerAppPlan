using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using FileUtilityHub_WinUI.Core.Services;

namespace FileUtilityHub_WinUI;

/// <summary>
/// The main content page displayed inside the application window.
/// Contains suite navigation and global status bar.
/// </summary>
public sealed partial class MainPage : Page
{
    public AppStatusService StatusService { get; }

    public MainPage()
    {
        StatusService = App.Current.Services.GetRequiredService<AppStatusService>();
        this.InitializeComponent();
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            // ContentFrame.Navigate(typeof(SettingsPage));
        }
        else
        {
            var navItemTag = args.SelectedItemContainer?.Tag?.ToString();
            if (navItemTag == "ImageOptimizer")
            {
                ContentFrame.Navigate(typeof(Features.ImageOptimizer.ImageOptimizerPage));
            }
            else if (navItemTag == "FileMergePdfBuilder")
            {
                ContentFrame.Navigate(typeof(Features.FileMerger.FileMergerPage));
            }
            else if (navItemTag == "PdfCompressor")
            {
                ContentFrame.Navigate(typeof(Features.PdfCompressor.PdfCompressorPage));
            }
            else if (navItemTag == "PdfConverter")
            {
                ContentFrame.Navigate(typeof(Features.PdfConverter.PdfConverterPage));
            }
            else if (navItemTag == "DocumentOcr")
            {
                ContentFrame.Navigate(typeof(Features.DocumentOcr.DocumentOcrPage));
            }
            else
            {
                ContentFrame.Navigate(typeof(PlaceholderPage), navItemTag);
            }
        }
    }
}
