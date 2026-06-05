namespace FileUtilityHub_WinUI.Core.Contracts;

/// <summary>
/// Abstraction for file/folder picker operations.
/// Keeps WinUI FileOpenPicker/FolderPicker/App.MainWindow out of ViewModel.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Show file open picker and return selected file paths.
    /// </summary>
    Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions);

    /// <summary>
    /// Show folder picker and return selected folder path, or null if cancelled.
    /// </summary>
    Task<string?> PickFolderAsync();
}
