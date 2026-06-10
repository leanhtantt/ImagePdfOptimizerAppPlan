namespace FileUtilityHub_WinUI.Infrastructure.Ffmpeg;

/// <summary>
/// Builds FFmpeg command arguments. Centralizes command string construction
/// to keep it out of services and ViewModels.
/// Ref: doc 07 section 11.3.
/// </summary>
public static class FfmpegCommandBuilder
{
    /// <summary>
    /// Build AVIF conversion command arguments.
    /// </summary>
    public static string BuildAvifConvertCommand(
        string inputPath, string outputPath,
        int crf, int cpuUsed, int maxLongEdge)
    {
        var vfScale = maxLongEdge > 0
            ? $"-vf \"scale='if(gt(iw,ih),min(iw,{maxLongEdge}),-2)':'if(gt(iw,ih),-2,min(ih,{maxLongEdge}))'\" "
            : "";

        return $"-y -i \"{inputPath}\" -frames:v 1 {vfScale}-c:v libaom-av1 -crf {crf} -cpu-used {cpuUsed} -pix_fmt yuv420p \"{outputPath}\"";
    }

    /// <summary>
    /// Build AVIF conversion command with explicit color mode support.
    /// Used by Convert PDF to produce grayscale or RGB AVIF intermediates.
    /// </summary>
    public static string BuildAvifConvertWithColorModeCommand(
        string inputPath, string outputPath,
        int crf, int cpuUsed, int maxLongEdge, string pixelFormat)
    {
        var filters = new System.Collections.Generic.List<string>();

        if (maxLongEdge > 0)
        {
            filters.Add($"scale='if(gt(iw,ih),min(iw,{maxLongEdge}),-2)':'if(gt(iw,ih),-2,min(ih,{maxLongEdge}))'");
        }

        if (pixelFormat == "gray")
        {
            filters.Add("format=gray");
        }

        var vfArg = filters.Count > 0
            ? $"-vf \"{string.Join(",", filters)}\" "
            : "";

        return $"-hide_banner -loglevel error -y -i \"{inputPath}\" -frames:v 1 {vfArg}-c:v libaom-av1 -crf {crf} -cpu-used {cpuUsed} -pix_fmt {pixelFormat} \"{outputPath}\"";
    }

    /// <summary>
    /// Build image probe command (future use).
    /// </summary>
    public static string BuildImageProbeCommand(string inputPath)
    {
        return $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 \"{inputPath}\"";
    }

    /// <summary>
    /// Build JPEG preparation command for PDF merge.
    /// Mirrors PowerShell Convert-ToTempJpeg: converts any image to JPEG
    /// with quality, resize, and color mode applied.
    /// </summary>
    public static string BuildJpegPrepareArgs(
        string inputPath, string outputPath,
        int qscale, int maxLongEdge, string pixelFormat)
    {
        var filters = new System.Collections.Generic.List<string>();

        if (maxLongEdge > 0)
        {
            filters.Add($"scale='if(gt(iw,ih),min(iw,{maxLongEdge}),-2)':'if(gt(iw,ih),-2,min(ih,{maxLongEdge}))'");
        }

        if (pixelFormat == "gray")
        {
            filters.Add("format=gray");
        }

        var vfArg = filters.Count > 0
            ? $"-vf \"{string.Join(",", filters)}\" "
            : "";

        return $"-hide_banner -loglevel error -y -i \"{inputPath}\" {vfArg}-frames:v 1 -q:v {qscale} -pix_fmt {pixelFormat} \"{outputPath}\"";
    }

    /// <summary>
    /// Build PNG conversion command (lossless).
    /// </summary>
    public static string BuildPngConvertCommand(string inputPath, string outputPath)
    {
        return $"-hide_banner -loglevel error -y -i \"{inputPath}\" -frames:v 1 \"{outputPath}\"";
    }

    /// <summary>
    /// Build WebP conversion command with quality setting.
    /// </summary>
    public static string BuildWebpConvertCommand(string inputPath, string outputPath, int quality)
    {
        return $"-hide_banner -loglevel error -y -i \"{inputPath}\" -frames:v 1 -c:v libwebp -quality {quality} \"{outputPath}\"";
    }
}
