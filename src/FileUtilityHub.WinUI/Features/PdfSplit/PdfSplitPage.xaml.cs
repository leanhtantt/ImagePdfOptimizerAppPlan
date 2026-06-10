using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FileUtilityHub_WinUI.Features.PdfSplit;

/// <summary>
/// Code-behind for PdfSplitPage.
/// Resolves ViewModel from DI and wires up selection-based commands.
/// </summary>
public sealed partial class PdfSplitPage : Page
{
    public PdfSplitViewModel ViewModel { get; }

    public PdfSplitPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<PdfSplitViewModel>();
        this.InitializeComponent();
    }

    private void OnRemoveSelectedClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FileList.SelectedItems.Count > 0)
        {
            ViewModel.RemoveItems(FileList.SelectedItems);
        }
    }
}
