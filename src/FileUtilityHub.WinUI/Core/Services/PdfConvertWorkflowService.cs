using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;
using PdfSharp.Drawing;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Orchestrates the Convert PDF pipeline for a single file:
/// Input → normalize to pages → AVIF optimize → combine into PDF output.
/// </summary>
public class PdfConvertWorkflowService
{
    private readonly FfmpegRunner _ffmpegRunner;
    private readonly PdfRenderService _pdfRenderService;
    private readonly OfficeConvertService _officeConvertService;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { "jpg", "jpeg", "png", "bmp", "tif", "tiff", "webp", "avif" };

    private static readonly HashSet<string> OfficeExtensions = new(StringComparer.OrdinalIgnoreCase)
        { "doc", "docx", "xls", "xlsx", "ppt", "pptx" };

    public PdfConvertWorkflowService(
        FfmpegRunner ffmpegRunner,
        PdfRenderService pdfRenderService,
        OfficeConvertService officeConvertService)
    {
        _ffmpegRunner = ffmpegRunner;
        _pdfRenderService = pdfRenderService;
        _officeConvertService = officeConvertService;
    }

    /// <summary>
    /// Converts a single input file to a PDF.
    /// Returns true on success, false on failure. Updates item status accordingly.
    /// </summary>
    public async Task<bool> ConvertFileAsync(
        PdfConvertItem item, PdfConvertConfig config, CancellationToken ct)
    {
        // Create a unique temp directory for this job
        var tempDir = Path.Combine(Path.GetTempPath(), $"fuh_convert_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            item.Status = ProcessingStatus.Processing;
            item.ErrorMessage = null;
            item.Warning = null;

            var ext = item.Format.ToLowerInvariant();

            // Step 1: Normalize input to page image files
            List<string> pageImagePaths;

            if (ImageExtensions.Contains(ext))
            {
                // Image input: the image itself is the single "page"
                pageImagePaths = new List<string> { item.SourcePath };
            }
            else if (ext == "pdf")
            {
                // PDF input: render each page to JPEG
                pageImagePaths = await RenderPdfPagesAsync(item.SourcePath, tempDir, config, ct);
            }
            else if (OfficeExtensions.Contains(ext))
            {
                // Office input: convert to temp PDF, then render pages
                pageImagePaths = await ConvertOfficeAndRenderAsync(item.SourcePath, tempDir, config, ct);
            }
            else
            {
                item.Status = ProcessingStatus.Error;
                item.ErrorMessage = $"Định dạng không hỗ trợ: .{ext}";
                return false;
            }

            if (ct.IsCancellationRequested) return false;

            if (pageImagePaths.Count == 0)
            {
                item.Status = ProcessingStatus.Error;
                item.ErrorMessage = "Không thể trích xuất trang nào từ file.";
                return false;
            }

            List<string> jpegPaths;

            if (config.SkipAvif)
            {
                // FAST PATH: Skip AVIF, convert directly to JPEG
                jpegPaths = new List<string>();
                for (int i = 0; i < pageImagePaths.Count; i++)
                {
                    if (ct.IsCancellationRequested) return false;

                    var jpegPath = Path.Combine(tempDir, $"page_{i:0000}.jpg");
                    var args = FfmpegCommandBuilder.BuildJpegPrepareArgs(
                        pageImagePaths[i], jpegPath,
                        config.JpegQScale, 0, config.JpegPixelFormat);

                    var success = await _ffmpegRunner.RunCommandAsync(args);
                    if (!success || !File.Exists(jpegPath))
                    {
                        item.Status = ProcessingStatus.Error;
                        item.ErrorMessage = $"JPEG conversion failed for page {i + 1}.";
                        return false;
                    }
                    jpegPaths.Add(jpegPath);
                }
            }
            else
            {
                // QUALITY PATH: Pages → AVIF optimize → JPEG
                var avifPaths = new List<string>();
                for (int i = 0; i < pageImagePaths.Count; i++)
                {
                    if (ct.IsCancellationRequested) return false;

                    var avifPath = Path.Combine(tempDir, $"page_{i:0000}.avif");
                    var args = FfmpegCommandBuilder.BuildAvifConvertWithColorModeCommand(
                        pageImagePaths[i], avifPath,
                        config.AvifCrf, config.AvifCpuUsed,
                        config.MaxLongEdge, config.AvifPixelFormat);

                    var success = await _ffmpegRunner.RunCommandAsync(args);
                    if (!success || !File.Exists(avifPath))
                    {
                        item.Status = ProcessingStatus.Error;
                        item.ErrorMessage = $"AVIF optimization failed for page {i + 1}.";
                        return false;
                    }
                    avifPaths.Add(avifPath);
                }

                jpegPaths = new List<string>();
                for (int i = 0; i < avifPaths.Count; i++)
                {
                    if (ct.IsCancellationRequested) return false;

                    var jpegPath = Path.Combine(tempDir, $"page_{i:0000}.jpg");
                    var args = FfmpegCommandBuilder.BuildJpegPrepareArgs(
                        avifPaths[i], jpegPath,
                        config.JpegQScale, 0, config.JpegPixelFormat);

                    var success = await _ffmpegRunner.RunCommandAsync(args);
                    if (!success || !File.Exists(jpegPath))
                    {
                        item.Status = ProcessingStatus.Error;
                        item.ErrorMessage = $"JPEG preparation failed for page {i + 1}.";
                        return false;
                    }
                    jpegPaths.Add(jpegPath);
                }
            }

            if (ct.IsCancellationRequested) return false;

            // Step 4: Build the PDF from JPEG pages
            var outputPath = GenerateOutputPath(item.SourcePath, config.ColorModeSuffix);
            var tempOutputPath = Path.Combine(tempDir, "output.pdf");

            BuildPdfFromJpegs(tempOutputPath, jpegPaths);

            if (!File.Exists(tempOutputPath))
            {
                item.Status = ProcessingStatus.Error;
                item.ErrorMessage = "Không thể tạo file PDF output.";
                return false;
            }

            // Step 5: Move to final location
            File.Copy(tempOutputPath, outputPath, overwrite: false);

            var outputInfo = new FileInfo(outputPath);
            item.OutputPath = outputPath;
            item.OutputSizeBytes = outputInfo.Length;

            // Check if output is larger than input
            if (item.OutputSizeBytes > item.OriginalSizeBytes)
            {
                item.Status = ProcessingStatus.Warning;
                item.Warning = "File PDF output nặng hơn file gốc.";
            }
            else
            {
                item.Status = ProcessingStatus.Success;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            item.Status = ProcessingStatus.Pending;
            return false;
        }
        catch (Exception ex)
        {
            item.Status = ProcessingStatus.Error;
            item.ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    private async Task<List<string>> RenderPdfPagesAsync(
        string pdfPath, string tempDir, PdfConvertConfig config, CancellationToken ct)
    {
        bool grayscale = config.ColorMode == PdfConvertColorMode.BlackWhite;
        return await _pdfRenderService.RenderPdfToJpegsAsync(
            pdfPath, tempDir, config.RenderDpi, config.RenderJpegQuality, grayscale, ct);
    }

    private async Task<List<string>> ConvertOfficeAndRenderAsync(
        string officePath, string tempDir, PdfConvertConfig config, CancellationToken ct)
    {
        // Step 1: Office → temp PDF
        var tempPdfPath = await _officeConvertService.ConvertToPdfAsync(officePath, tempDir, ct);

        if (string.IsNullOrEmpty(tempPdfPath) || !File.Exists(tempPdfPath))
        {
            throw new Exception("Office conversion failed. Kiểm tra Microsoft Office đã cài đặt.");
        }

        // Step 2: Render temp PDF pages
        return await RenderPdfPagesAsync(tempPdfPath, tempDir, config, ct);
    }

    private static void BuildPdfFromJpegs(string outputPath, List<string> jpegPaths)
    {
        using var doc = new PdfSharp.Pdf.PdfDocument();

        foreach (var jpegPath in jpegPaths)
        {
            using var image = XImage.FromFile(jpegPath);

            var page = doc.AddPage();
            // Page size matches image dimensions (in points)
            page.Width = XUnit.FromPoint(image.PixelWidth);
            page.Height = XUnit.FromPoint(image.PixelHeight);

            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawImage(image, 0, 0, image.PixelWidth, image.PixelHeight);
        }

        if (doc.PageCount > 0)
            doc.Save(outputPath);
    }

    /// <summary>
    /// Generates output path with color mode suffix and auto-increment to avoid overwriting.
    /// Example: BaoCao.docx → BaoCao_blackwhite.pdf
    /// </summary>
    internal static string GenerateOutputPath(string sourcePath, string colorModeSuffix)
    {
        var dir = Path.GetDirectoryName(sourcePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var outputName = $"{baseName}_{colorModeSuffix}.pdf";
        var outputPath = Path.Combine(dir, outputName);

        if (!File.Exists(outputPath))
            return outputPath;

        // Auto-increment suffix
        for (int suffix = 1; ; suffix++)
        {
            outputPath = Path.Combine(dir, $"{baseName}_{colorModeSuffix}_{suffix}.pdf");
            if (!File.Exists(outputPath))
                return outputPath;
        }
    }
}
