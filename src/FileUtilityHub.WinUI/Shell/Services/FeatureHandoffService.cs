using FileUtilityHub_WinUI.Core.Contracts;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Shell.Services;

/// <summary>
/// Shell-level implementation of feature handoff.
/// Lives in Shell layer (not Core) because it accesses WinUI navigation directly.
/// Core only has the IFeatureHandoffService contract.
/// </summary>
public class FeatureHandoffService : IFeatureHandoffService
{
    // Store last context so target feature can read it
    public FileBatchContext? PendingMergeContext { get; internal set; }
    public bool HasPendingAutomation { get; private set; }

    public void NavigateToMerge(FileBatchContext context)
    {
        PendingMergeContext = context;
        HasPendingAutomation = false;
        NavigateToModule("FileMergePdfBuilder");
    }

    public void NavigateToMergeAndCompress(FileBatchContext context)
    {
        PendingMergeContext = context;
        HasPendingAutomation = true;
        NavigateToModule("FileMergePdfBuilder");
    }

    private static void NavigateToModule(string moduleTag)
    {
        // Access shell navigation — this is why this service is in Shell, not Core
        if (App.MainWindow?.Content is Microsoft.UI.Xaml.Controls.Frame rootFrame &&
            rootFrame.Content is Microsoft.UI.Xaml.Controls.Page mainPage)
        {
            if (mainPage.FindName("ContentFrame") is Microsoft.UI.Xaml.Controls.Frame contentFrame)
            {
                contentFrame.Navigate(typeof(Features.FileMerger.FileMergerPage), moduleTag);
            }

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
}
