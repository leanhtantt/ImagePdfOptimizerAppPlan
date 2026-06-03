using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using FileUtilityHub_WinUI.Core.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileUtilityHub_WinUI;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
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
            if (navItemTag == "ImagePdfOptimizer")
            {
                ContentFrame.Navigate(typeof(Features.ImagePdfOptimizer.ImagePdfOptimizerPage));
            }
            else
            {
                ContentFrame.Navigate(typeof(PlaceholderPage), navItemTag);
            }
        }
    }
}
