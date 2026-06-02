using System.IO;

namespace FileUtilityHub_WinUI.Infrastructure.FileSystem;

public class OutputManager
{
    public string CreateAvifOutputDirectory(string sourceFolder)
    {
        var outDir = Path.Combine(sourceFolder, "compressed-avif");
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        return outDir;
    }

    public string CreatePdfOutputDirectory(string sourceFolder)
    {
        var outDir = Path.Combine(sourceFolder, "pdf-output");
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        return outDir;
    }

    public string GeneratePdfFileName(string sourceFolder, int q, string colorMode)
    {
        var folderName = new DirectoryInfo(sourceFolder).Name;
        return $"{folderName}-q{q}-{colorMode}.pdf";
    }
}
