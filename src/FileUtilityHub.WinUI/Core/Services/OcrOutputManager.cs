using System.IO;
using System.Threading.Tasks;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Manages DOCX output file naming and safe writing.
/// Ensures no overwriting of existing files.
/// </summary>
public sealed class OcrOutputManager
{
    /// <summary>
    /// Get output path for a source file: {name}_ocr.docx in the same directory.
    /// If already exists, appends _1, _2, etc.
    /// </summary>
    public string GetOutputPath(string sourcePath)
    {
        var dir = Path.GetDirectoryName(sourcePath) ?? ".";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
        var baseName = $"{nameWithoutExt}_ocr";
        var outputPath = Path.Combine(dir, $"{baseName}.docx");

        if (!File.Exists(outputPath))
            return outputPath;

        int counter = 1;
        while (File.Exists(outputPath))
        {
            outputPath = Path.Combine(dir, $"{baseName}_{counter}.docx");
            counter++;
        }

        return outputPath;
    }

    /// <summary>
    /// Save DOCX stream to file atomically (write temp, then rename).
    /// </summary>
    public async Task SaveDocxAsync(Stream docxStream, string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath) ?? ".";
        var tempPath = Path.Combine(dir, $".tmp_{Path.GetFileName(outputPath)}");

        try
        {
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await docxStream.CopyToAsync(fileStream);
            }

            // Atomic rename
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            File.Move(tempPath, outputPath);
        }
        catch
        {
            // Cleanup temp on failure
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best effort */ }
            }
            throw;
        }
    }
}
