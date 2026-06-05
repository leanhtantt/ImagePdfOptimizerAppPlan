using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace FileUtilityHub_WinUI.Shared.Controls;

/// <summary>
/// Shared drag-and-drop zone control.
/// Provides visual feedback on DragOver and invokes DropCommand with dropped IStorageItems.
/// Reusable across all features that accept file/folder input.
/// </summary>
public sealed partial class DropZoneControl : UserControl
{
    public DropZoneControl()
    {
        this.InitializeComponent();
    }

    // Command invoked when files are dropped; parameter is IReadOnlyList<IStorageItem>
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(DropZoneControl),
            new PropertyMetadata(null));

    public ICommand DropCommand
    {
        get => (ICommand)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Thả để thêm file";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;

        // Visual feedback
        DropBorder.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        DropIcon.Glyph = "\uE74B"; // Download icon
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        // Reset visual
        DropBorder.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        DropIcon.Glyph = "\uE896"; // Upload icon
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        // Reset visual
        DropBorder.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        DropIcon.Glyph = "\uE896";

        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0 && DropCommand?.CanExecute(items) == true)
            {
                DropCommand.Execute(items);
            }
        }
    }
}
