using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FileUtilityHub_WinUI.Features.PdfCompressor;

public sealed partial class PdfCompressorPage : Page
{
    public PdfCompressorViewModel ViewModel { get; }

    public PdfCompressorPage()
    {
        this.InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<PdfCompressorViewModel>();
    }

    private void ThumbnailList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Core.Models.PdfPageItem clickedPage)
        {
            ViewModel.SelectedPreviewPage = clickedPage;

            var container = MainItemsControl.ContainerFromItem(clickedPage) as Microsoft.UI.Xaml.FrameworkElement;
            if (container != null)
            {
                container.StartBringIntoView(new Microsoft.UI.Xaml.BringIntoViewOptions
                {
                    AnimationDesired = true,
                    VerticalAlignmentRatio = 0
                });
            }
        }
    }

    private void PreviewPage_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is Core.Models.PdfPageItem page)
        {
            page.IsSelected = !page.IsSelected;
        }
    }
}
