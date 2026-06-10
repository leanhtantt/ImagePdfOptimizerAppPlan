using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Features.DocumentOcr;

public sealed partial class DocumentOcrPage : Page
{
    public DocumentOcrViewModel ViewModel { get; }

    public DocumentOcrPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<DocumentOcrViewModel>();
        this.InitializeComponent();
        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void OnRemoveSelectedClick(object sender, RoutedEventArgs e)
    {
        var selected = FileList.SelectedItems.Cast<object>().ToList();
        ViewModel.RemoveSelectedCommand.Execute(selected);
    }

    private void OnOcrClick(object sender, RoutedEventArgs e)
    {
        var selected = FileList.SelectedItems.Cast<object>().ToList();
        ViewModel.OcrToWordCommand.Execute(selected);
    }
}
