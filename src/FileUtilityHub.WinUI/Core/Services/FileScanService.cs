using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Core.Services;

public class FileScanService
{
    private readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".avif", ".bmp", ".tif", ".tiff", ".pdf" };

    public (List<ImageItem> ValidFiles, List<string> InvalidFiles) ScanDirectory(string folderPath)
    {
        var validFiles = new List<ImageItem>();
        var invalidFiles = new List<string>();

        if (!Directory.Exists(folderPath))
            return (validFiles, invalidFiles);

        var files = Directory.GetFiles(folderPath);
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (_supportedExtensions.Contains(ext))
            {
                var fileInfo = new FileInfo(file);
                validFiles.Add(new ImageItem
                {
                    FileName = fileInfo.Name,
                    SourcePath = fileInfo.FullName,
                    Format = ext.TrimStart('.'),
                    OriginalSizeBytes = fileInfo.Length
                });
            }
            else
            {
                invalidFiles.Add(file);
            }
        }

        validFiles = validFiles.OrderBy(f => f.FileName).ToList();
        return (validFiles, invalidFiles);
    }
}
