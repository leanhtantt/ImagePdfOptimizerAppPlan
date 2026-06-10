namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Configuration for PDF Split operations.
/// Controls DPI, output format, and quality settings.
/// </summary>
public class PdfSplitConfig
{
    /// <summary>
    /// DPI preset selection index:
    /// 0 = Auto (150 DPI), 1 = Thấp (96), 2 = Cao (300), 3 = Tùy chỉnh
    /// </summary>
    public int DpiPresetIndex { get; set; } = 0;

    /// <summary>
    /// Custom DPI value, used when DpiPresetIndex == 3.
    /// </summary>
    public int CustomDpi { get; set; } = 150;

    /// <summary>
    /// Computed effective DPI based on preset or custom value.
    /// </summary>
    public int EffectiveDpi => DpiPresetIndex switch
    {
        0 => 150,  // Auto
        1 => 96,   // Thấp
        2 => 300,  // Cao
        3 => CustomDpi,
        _ => 150
    };

    /// <summary>
    /// Output image format index:
    /// 0 = JPEG, 1 = PNG, 2 = AVIF, 3 = WebP
    /// </summary>
    public int OutputFormatIndex { get; set; } = 0;

    public SplitOutputFormat OutputFormat => OutputFormatIndex switch
    {
        0 => SplitOutputFormat.Jpeg,
        1 => SplitOutputFormat.Png,
        2 => SplitOutputFormat.Avif,
        3 => SplitOutputFormat.WebP,
        _ => SplitOutputFormat.Jpeg
    };

    /// <summary>
    /// JPEG quality (1–100). Used when OutputFormat == Jpeg.
    /// </summary>
    public int JpegQuality { get; set; } = 90;

    /// <summary>
    /// AVIF CRF (0–63). Lower = better quality. Used when OutputFormat == Avif.
    /// </summary>
    public int AvifCrf { get; set; } = 28;

    /// <summary>
    /// WebP quality (0–100). Used when OutputFormat == WebP.
    /// </summary>
    public int WebPQuality { get; set; } = 85;

    /// <summary>
    /// File extension for the output format (without dot).
    /// </summary>
    public string OutputExtension => OutputFormat switch
    {
        SplitOutputFormat.Jpeg => "jpg",
        SplitOutputFormat.Png => "png",
        SplitOutputFormat.Avif => "avif",
        SplitOutputFormat.WebP => "webp",
        _ => "jpg"
    };
}

public enum SplitOutputFormat
{
    Jpeg,
    Png,
    Avif,
    WebP
}
