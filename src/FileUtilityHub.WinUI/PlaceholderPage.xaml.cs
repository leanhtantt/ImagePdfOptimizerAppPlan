using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;

namespace FileUtilityHub_WinUI;

/// <summary>
/// Placeholder page for modules not yet implemented.
/// Receives the module tag as navigation parameter and displays the module name.
/// </summary>
public sealed partial class PlaceholderPage : Page
{
    private static readonly Dictionary<string, string> ModuleNames = new()
    {
        ["PdfSplit"] = "Tách PDF",
    };

    public PlaceholderPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string tag && ModuleNames.TryGetValue(tag, out var name))
        {
            ModuleTitle.Text = name;
        }
        else
        {
            ModuleTitle.Text = e.Parameter?.ToString() ?? "Module";
        }
    }
}
