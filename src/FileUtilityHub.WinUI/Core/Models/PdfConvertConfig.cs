namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Color mode for the Convert PDF feature.
/// </summary>
public enum PdfConvertColorMode
{
    /// <summary>Đen trắng - mặc định. Loại bỏ màu, giống bản scan.</summary>
    BlackWhite,

    /// <summary>RGB - giữ màu gần file gốc.</summary>
    Rgb
}

/// <summary>
/// Configuration for a single Convert PDF job.
/// Uses internal presets — no DPI/CRF/JpegQ exposed to users in MVP.
/// </summary>
public sealed class PdfConvertConfig
{
    public PdfConvertColorMode ColorMode { get; set; } = PdfConvertColorMode.BlackWhite;

    // --- Internal presets (not exposed to UI in MVP) ---

    /// <summary>DPI for rendering PDF/Office pages.</summary>
    public int RenderDpi { get; set; } = 200;

    /// <summary>AVIF CRF for intermediate optimization.</summary>
    public int AvifCrf { get; set; } = 24;

    /// <summary>AVIF encoder cpu-used parameter.</summary>
    public int AvifCpuUsed { get; set; } = 4;

    /// <summary>Max long edge for AVIF. 0 = keep original.</summary>
    public int MaxLongEdge { get; set; } = 0;

    /// <summary>
    /// When true, skip the slow AVIF intermediate step.
    /// Goes directly Pages → JPEG → PDF. Much faster, slightly larger output.
    /// </summary>
    public bool SkipAvif { get; set; } = false;

    /// <summary>JPEG quality for the final PDF render (qscale 0-31, lower = better).</summary>
    public int JpegQScale { get; set; } = 6;

    /// <summary>JPEG quality for PDF page render (1-100).</summary>
    public int RenderJpegQuality { get; set; } = 85;

    /// <summary>
    /// Gets the FFmpeg pixel format for the AVIF step.
    /// </summary>
    public string AvifPixelFormat => ColorMode == PdfConvertColorMode.BlackWhite ? "gray" : "yuv420p";

    /// <summary>
    /// Gets the FFmpeg pixel format for the JPEG PDF embed step.
    /// </summary>
    public string JpegPixelFormat => ColorMode == PdfConvertColorMode.BlackWhite ? "gray" : "yuvj420p";

    /// <summary>
    /// Gets the output filename suffix based on color mode.
    /// </summary>
    public string ColorModeSuffix => ColorMode == PdfConvertColorMode.BlackWhite ? "blackwhite" : "rgb";
}
