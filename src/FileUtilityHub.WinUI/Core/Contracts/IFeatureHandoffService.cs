using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Core.Contracts;

/// <summary>
/// Contract for navigating between features with context.
/// Features don't call each other's UI directly; they go through this service.
/// Ref: doc 09 section 4-7.
/// </summary>
public interface IFeatureHandoffService
{
    /// <summary>
    /// Navigate to File Merge / PDF Builder with a batch of files.
    /// </summary>
    void NavigateToMerge(FileBatchContext context);

    /// <summary>
    /// Run automation: Image Optimizer -> File Merge -> PDF Compressor.
    /// </summary>
    void NavigateToMergeAndCompress(FileBatchContext context);
}
