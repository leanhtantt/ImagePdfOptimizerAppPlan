using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Builds a PDF from prepared JPEG images using raw PDF structure.
/// Ported from PowerShell New-ImagePdf function.
/// No external PDF library required.
/// </summary>
public class PdfBuilderService
{
    private readonly FfmpegRunner _ffmpegRunner;

    // A4 dimensions in PDF points (1 point = 1/72 inch)
    private const double A4Width = 595.28;
    private const double A4Height = 841.89;

    public PdfBuilderService(FfmpegRunner ffmpegRunner)
    {
        _ffmpegRunner = ffmpegRunner;
    }

    /// <summary>
    /// Prepares a single image by converting it to a temporary JPEG via FFmpeg.
    /// Mirrors PowerShell Convert-ToTempJpeg.
    /// </summary>
    public async Task<bool> PrepareImageAsync(
        MergeFileItem item, PdfMergeConfig config, string tempDir, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return false;

        var tempJpgPath = Path.Combine(tempDir, $"{item.OrderIndex:0000}.jpg");

        var args = FfmpegCommandBuilder.BuildJpegPrepareArgs(
            item.SourcePath,
            tempJpgPath,
            config.EffectiveQScale,
            config.MaxLongEdge,
            config.PixelFormat);

        var success = await _ffmpegRunner.RunCommandAsync(args);

        if (!success || !File.Exists(tempJpgPath))
        {
            item.Status = ProcessingStatus.Error;
            item.ErrorMessage = "FFmpeg failed to prepare image";
            return false;
        }

        // Read JPEG dimensions from file header
        var (width, height) = ReadJpegDimensions(tempJpgPath);
        item.PreparedJpegPath = tempJpgPath;
        item.ImageWidth = width;
        item.ImageHeight = height;
        item.Status = ProcessingStatus.Success;

        return true;
    }

    /// <summary>
    /// Builds a PDF file from a list of prepared JPEG images.
    /// Ported from PowerShell New-ImagePdf function.
    /// </summary>
    public void BuildPdf(string outputPath, IReadOnlyList<MergeFileItem> items, PdfMergeConfig config)
    {
        var pageCount = items.Count;
        var objectCount = 2 + (pageCount * 3); // catalog + pages + (page + image + content) per image
        var offsets = new long[objectCount + 1];

        using var stream = new MemoryStream();

        // PDF header
        WriteAscii(stream, "%PDF-1.4\n%Image PDF\n");

        // Object 1: Catalog
        AddPdfObject(stream, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");

        // Object 2: Pages
        var kids = new StringBuilder();
        for (int i = 0; i < pageCount; i++)
        {
            var pageObj = 3 + (i * 3);
            if (i > 0) kids.Append(' ');
            kids.Append($"{pageObj} 0 R");
        }
        AddPdfObject(stream, offsets, 2, $"<< /Type /Pages /Count {pageCount} /Kids [ {kids} ] >>");

        // Per-page objects
        for (int i = 0; i < pageCount; i++)
        {
            var item = items[i];
            var pageObj = 3 + (i * 3);
            var imageObj = pageObj + 1;
            var contentObj = pageObj + 2;

            var imgWidth = (double)(item.ImageWidth ?? 100);
            var imgHeight = (double)(item.ImageHeight ?? 100);

            CalculatePageLayout(config.PageMode, imgWidth, imgHeight,
                out var pageWidth, out var pageHeight,
                out var drawWidth, out var drawHeight,
                out var drawX, out var drawY);

            // Image XObject (embed raw JPEG bytes)
            var jpegBytes = File.ReadAllBytes(item.PreparedJpegPath!);
            var colorSpace = config.ColorMode == PdfColorMode.Grayscale ? "/DeviceGray" : "/DeviceRGB";
            var imageDict = $"<< /Type /XObject /Subtype /Image /Width {item.ImageWidth} /Height {item.ImageHeight} /ColorSpace {colorSpace} /BitsPerComponent 8 /Filter /DCTDecode /Length {jpegBytes.Length} >>";
            AddPdfStreamObject(stream, offsets, imageObj, imageDict, jpegBytes);

            // Content stream (position and scale the image)
            var content = $"q\n{drawWidth:F2} 0 0 {drawHeight:F2} {drawX:F2} {drawY:F2} cm\n/Im{i} Do\nQ\n";
            var contentBytes = Encoding.ASCII.GetBytes(content);
            AddPdfStreamObject(stream, offsets, contentObj, $"<< /Length {contentBytes.Length} >>", contentBytes);

            // Page object
            var mediaBox = $"0 0 {pageWidth:F2} {pageHeight:F2}";
            var pageText = $"<< /Type /Page /Parent 2 0 R /MediaBox [ {mediaBox} ] /Resources << /XObject << /Im{i} {imageObj} 0 R >> >> /Contents {contentObj} 0 R >>";
            AddPdfObject(stream, offsets, pageObj, pageText);
        }

        // Cross-reference table
        var xrefPosition = stream.Position;
        WriteAscii(stream, $"xref\n0 {objectCount + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        for (int i = 1; i <= objectCount; i++)
        {
            WriteAscii(stream, $"{offsets[i]:D10} 00000 n \n");
        }
        WriteAscii(stream, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");

        File.WriteAllBytes(outputPath, stream.ToArray());
    }

    /// <summary>
    /// Calculates page dimensions and image draw position based on PageMode.
    /// Ported from PowerShell New-ImagePdf page layout logic.
    /// </summary>
    private static void CalculatePageLayout(
        PdfPageMode mode, double imgWidth, double imgHeight,
        out double pageWidth, out double pageHeight,
        out double drawWidth, out double drawHeight,
        out double drawX, out double drawY)
    {
        switch (mode)
        {
            case PdfPageMode.ImageSize:
                pageWidth = imgWidth;
                pageHeight = imgHeight;
                drawWidth = imgWidth;
                drawHeight = imgHeight;
                drawX = 0;
                drawY = 0;
                break;

            case PdfPageMode.A4Fit:
                pageWidth = A4Width;
                pageHeight = A4Height;
                var fitScale = Math.Min(pageWidth / imgWidth, pageHeight / imgHeight);
                drawWidth = Math.Round(imgWidth * fitScale, 2);
                drawHeight = Math.Round(imgHeight * fitScale, 2);
                drawX = Math.Round((pageWidth - drawWidth) / 2, 2);
                drawY = Math.Round((pageHeight - drawHeight) / 2, 2);
                break;

            case PdfPageMode.A4Full:
            default:
                pageWidth = A4Width;
                pageHeight = A4Height;
                var coverScale = Math.Max(pageWidth / imgWidth, pageHeight / imgHeight);
                drawWidth = Math.Round(imgWidth * coverScale, 2);
                drawHeight = Math.Round(imgHeight * coverScale, 2);
                drawX = Math.Round((pageWidth - drawWidth) / 2, 2);
                drawY = Math.Round((pageHeight - drawHeight) / 2, 2);
                break;
        }
    }

    /// <summary>
    /// Reads JPEG dimensions from file header without loading full image.
    /// Falls back to default if header cannot be parsed.
    /// </summary>
    private static (int width, int height) ReadJpegDimensions(string jpegPath)
    {
        try
        {
            using var fs = File.OpenRead(jpegPath);
            using var reader = new BinaryReader(fs);

            // Verify JPEG SOI marker
            if (reader.ReadByte() != 0xFF || reader.ReadByte() != 0xD8)
                return (100, 100);

            while (fs.Position < fs.Length - 1)
            {
                if (reader.ReadByte() != 0xFF) continue;

                byte marker = reader.ReadByte();

                // Skip padding FF bytes
                while (marker == 0xFF && fs.Position < fs.Length)
                    marker = reader.ReadByte();

                // SOF markers (Start Of Frame) contain dimensions
                if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xCC)
                {
                    // Read segment length (skip)
                    reader.ReadByte(); reader.ReadByte();
                    // Read precision (skip)
                    reader.ReadByte();
                    // Read height (big-endian 16-bit)
                    int height = (reader.ReadByte() << 8) | reader.ReadByte();
                    // Read width (big-endian 16-bit)
                    int width = (reader.ReadByte() << 8) | reader.ReadByte();

                    if (width > 0 && height > 0)
                        return (width, height);
                }
                else if (marker != 0x00 && marker != 0x01 && !(marker >= 0xD0 && marker <= 0xD9))
                {
                    // Skip segment
                    int segLen = (reader.ReadByte() << 8) | reader.ReadByte();
                    if (segLen > 2)
                        fs.Seek(segLen - 2, SeekOrigin.Current);
                }
            }
        }
        catch
        {
            // Fallback
        }

        return (100, 100);
    }

    private static void WriteAscii(MemoryStream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void AddPdfObject(MemoryStream stream, long[] offsets, int objectId, string text)
    {
        offsets[objectId] = stream.Position;
        WriteAscii(stream, $"{objectId} 0 obj\n{text}\nendobj\n");
    }

    private static void AddPdfStreamObject(MemoryStream stream, long[] offsets, int objectId, string dictionary, byte[] data)
    {
        offsets[objectId] = stream.Position;
        WriteAscii(stream, $"{objectId} 0 obj\n{dictionary}\nstream\n");
        stream.Write(data, 0, data.Length);
        WriteAscii(stream, "\nendstream\nendobj\n");
    }
}
