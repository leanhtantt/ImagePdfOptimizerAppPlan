namespace FileUtilityHub.Core.Models;

public sealed class ImageConvertConfig
{
    public int AvifCrf { get; init; } = 24;
    public int AvifCpuUsed { get; init; } = 4;
    public int MaxLongEdge { get; init; } = 0; // 0 means keep original
    public bool SkipIfOutputLarger { get; init; } = true;
}

public sealed class PdfConfig
{
    public string PageMode { get; init; } = "image_size";
    public string ColorMode { get; init; } = "rgb";
    public int JpegQ { get; init; } = 12;
}
