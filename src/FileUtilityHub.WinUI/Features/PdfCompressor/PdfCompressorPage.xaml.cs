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
            var container = MainItemsControl.ContainerFromItem(clickedPage) as Microsoft.UI.Xaml.UIElement;
            if (container != null)
            {
                var transform = container.TransformToVisual(MainItemsControl);
                var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                MainScrollViewer.ChangeView(null, point.Y, null);
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
