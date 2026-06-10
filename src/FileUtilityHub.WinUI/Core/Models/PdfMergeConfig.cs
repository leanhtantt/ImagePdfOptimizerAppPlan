namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Page sizing mode for PDF output.
/// Maps to PowerShell script's -PageMode parameter.
/// </summary>
public enum PdfPageMode
{
    /// <summary>Each page matches the image's own pixel dimensions.</summary>
    ImageSize,

    /// <summary>A4 page, image scaled to cover the full page (may crop edges).</summary>
    A4Full,

    /// <summary>A4 page, image scaled to fit entirely (may leave white bars).</summary>
    A4Fit
}


/// <summary>
/// Color mode for PDF images.
/// Maps to PowerShell script's -ColorMode parameter.
/// </summary>
public enum PdfColorMode
{
    Rgb,
    Grayscale
}

public enum MergeInputProfile
{
    ImageOnly,
    ScannedDocuments
}

/// <summary>
/// Configuration for the image-to-PDF merge operation.
/// </summary>
public sealed class PdfMergeConfig
{
    public MergeInputProfile InputProfile { get; set; } = MergeInputProfile.ImageOnly;

    public PdfPageMode PageMode { get; set; } = PdfPageMode.ImageSize;
    public PdfColorMode ColorMode { get; set; } = PdfColorMode.Rgb;

    /// <summary>
    /// Copies PDF pages directly instead of rendering them to JPEG first.
    /// This preserves text/vector quality and avoids inflating already-optimized PDFs.
    /// </summary>
    public bool PreservePdfPages { get; set; } = true;

    /// <summary>
    /// ScannedDocuments profile: DPI setting for rendering (100-400)
    /// </summary>
    public int Dpi { get; set; } = 200;

    /// <summary>
    /// ScannedDocuments profile: JPEG Quality (40-95)
    /// </summary>
    public int JpegQuality { get; set; } = 85;

    /// <summary>
    /// ImageOnly profile: JPEG qscale override (0-31).
    /// Maps to FFmpeg -qscale:v
    /// </summary>
    public int JpegQScale { get; set; } = 0;

    /// <summary>
    /// Gets the effective qscale value, resolving auto (0) to preset.
    /// </summary>
    public int EffectiveQScale => JpegQScale > 0 ? JpegQScale : 6;

    /// <summary>
    /// Max long edge in pixels. 0 = keep original dimensions.
    /// Maps to PowerShell -MaxLongEdge parameter.
    /// </summary>
    public int MaxLongEdge { get; set; } = 0;

    /// <summary>
    /// Output PDF file name (not full path).
    /// </summary>
    public string OutputFileName { get; set; } = "combined-images.pdf";


    /// <summary>
    /// Gets the FFmpeg pixel format string.
    /// </summary>
    public string PixelFormat => ColorMode == PdfColorMode.Grayscale ? "gray" : "yuvj420p";
}
