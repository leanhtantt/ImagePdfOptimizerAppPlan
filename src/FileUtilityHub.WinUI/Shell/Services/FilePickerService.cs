using FileUtilityHub_WinUI.Core.Contracts;
using Windows.Storage.Pickers;

namespace FileUtilityHub_WinUI.Shell.Services;

/// <summary>
/// Shell-level file picker implementation using WinUI FileOpenPicker/FolderPicker.
/// Accesses App.MainWindow for hwnd — belongs in Shell, not Core.
/// </summary>
public class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions)
    {
        var picker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        foreach (var ext in extensions)
        {
            picker.FileTypeFilter.Add(ext.StartsWith('.') ? ext : $".{ext}");
        }

        var files = await picker.PickMultipleFilesAsync();
        return files.Select(f => f.Path).ToList();
    }

    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
