namespace FileUtilityHub.Core.Models;

public sealed class PdfVersion
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public int JpegQ { get; init; }
    public string ColorMode { get; init; } = "rgb";
    public string PageMode { get; init; } = "image_size";
    public bool IsFinal { get; set; }
    public List<string> Warnings { get; init; } = new();
}
