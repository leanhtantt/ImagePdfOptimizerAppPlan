using FileUtilityHub.Core.Contracts;
using FileUtilityHub.Core.Models;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Initial implementation of feature handoff. 
/// For now, navigates to PlaceholderPage with context info.
/// Will be upgraded when File Merge / PDF Compressor features are implemented.
/// </summary>
public class FeatureHandoffService : IFeatureHandoffService
{
    // Store last context so target feature can read it
    public FileBatchContext? PendingMergeContext { get; private set; }
    public bool HasPendingAutomation { get; private set; }

    public void NavigateToMerge(FileBatchContext context)
    {
        PendingMergeContext = context;
        HasPendingAutomation = false;

        // Navigate to File Merge / PDF Builder
        // For now: navigate to placeholder
        var mainPage = GetMainPage();
        if (mainPage != null)
        {
            NavigateToModule(mainPage, "FileMergePdfBuilder");
        }
    }

    public void NavigateToMergeAndCompress(FileBatchContext context)
    {
        PendingMergeContext = context;
        HasPendingAutomation = true;

        // Navigate to File Merge first, then auto-forward to PDF Compressor
        var mainPage = GetMainPage();
        if (mainPage != null)
        {
            NavigateToModule(mainPage, "FileMergePdfBuilder");
        }
    }

    private static Microsoft.UI.Xaml.Controls.Page? GetMainPage()
    {
        if (App.MainWindow?.Content is Microsoft.UI.Xaml.Controls.Frame rootFrame)
        {
            return rootFrame.Content as Microsoft.UI.Xaml.Controls.Page;
        }
        return null;
    }

    private static void NavigateToModule(Microsoft.UI.Xaml.Controls.Page mainPage, string moduleTag)
    {
        // Find the NavigationView and its ContentFrame
        if (mainPage.FindName("ContentFrame") is Microsoft.UI.Xaml.Controls.Frame contentFrame)
        {
            contentFrame.Navigate(typeof(PlaceholderPage), moduleTag);
        }

        // Update NavigationView selected item
        if (mainPage.FindName("NavView") is Microsoft.UI.Xaml.Controls.NavigationView navView)
        {
            foreach (var menuItem in navView.MenuItems)
            {
                if (menuItem is Microsoft.UI.Xaml.Controls.NavigationViewItem navItem &&
                    navItem.Tag?.ToString() == moduleTag)
                {
                    navView.SelectedItem = navItem;
                    break;
                }
            }
        }
    }
}
