using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Builds a PDF from prepared JPEG images using raw PDF structure.
/// Orchestrates conversion of Images, PDFs, and Office files.
/// </summary>
public class PdfBuilderService
{
    private readonly FfmpegRunner _ffmpegRunner;
    private readonly PdfRenderService _pdfRenderService;

    // A4 dimensions in PDF points (1 point = 1/72 inch)
    private const double A4Width = 595.28;
    private const double A4Height = 841.89;

    public PdfBuilderService(
        FfmpegRunner ffmpegRunner,
        PdfRenderService pdfRenderService)
    {
        _ffmpegRunner = ffmpegRunner;
        _pdfRenderService = pdfRenderService;
    }

    /// <summary>
    /// Prepares an item (Image, PDF, Office) by generating intermediate JPEGs.
    /// </summary>
    public async Task<bool> PrepareItemAsync(
        MergeFileItem item, PdfMergeConfig config, string tempDir, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return false;

        item.PreparedJpegPaths.Clear();

        try
        {
            var ext = item.Format.ToLowerInvariant();
            bool isImage = ext is "jpg" or "jpeg" or "png" or "webp" or "avif" or "bmp" or "tif" or "tiff" or "heic";
            bool isPdf = ext is "pdf";

            if (isImage)
            {
                var tempJpgPath = Path.Combine(tempDir, $"{item.OrderIndex:0000}_{Guid.NewGuid():N}.jpg");

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

                item.PreparedJpegPaths.Add(tempJpgPath);
                item.PageCount = 1;
            }
            else if (isPdf)
            {
                if (config.PreservePdfPages)
                {
                    using var pdf = PdfReader.Open(item.SourcePath, PdfDocumentOpenMode.Import);
                    item.PageCount = pdf.PageCount;
                    item.Status = ProcessingStatus.Success;
                    return true;
                }

                // Render PDF to JPEGs
                bool grayscale = config.ColorMode == PdfColorMode.Grayscale;
                var jpegPaths = await _pdfRenderService.RenderPdfToJpegsAsync(
                    item.SourcePath, tempDir, config.Dpi, config.JpegQuality, grayscale, ct);

                if (jpegPaths.Count == 0)
                {
                    item.Status = ProcessingStatus.Error;
                    item.ErrorMessage = "No pages could be rendered from the document.";
                    return false;
                }

                item.PreparedJpegPaths.AddRange(jpegPaths);
                item.PageCount = jpegPaths.Count;
            }
            else
            {
                item.Status = ProcessingStatus.Error;
                item.ErrorMessage = $"Unsupported format: {ext}";
                return false;
            }

            // Successfully got at least one JPEG
            item.Status = ProcessingStatus.Success;
            return true;
        }
        catch (Exception ex)
        {
            item.Status = ProcessingStatus.Error;
            item.ErrorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Builds a PDF while preserving source PDF pages when requested.
    /// </summary>
    public void BuildPdf(string outputPath, IReadOnlyList<MergeFileItem> items, PdfMergeConfig config)
    {
        using var outputDoc = new PdfSharp.Pdf.PdfDocument();

        foreach (var item in items.OrderBy(i => i.OrderIndex))
        {
            if (config.PreservePdfPages
                && item.Format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var inputDoc = PdfReader.Open(item.SourcePath, PdfDocumentOpenMode.Import);
                for (var pageIndex = 0; pageIndex < inputDoc.PageCount; pageIndex++)
                    outputDoc.AddPage(inputDoc.Pages[pageIndex]);

                continue;
            }

            foreach (var jpegPath in item.PreparedJpegPaths)
            {
                using var image = XImage.FromFile(jpegPath);
                CalculatePageLayout(config.PageMode, image.PixelWidth, image.PixelHeight,
                    out var pageWidth, out var pageHeight,
                    out var drawWidth, out var drawHeight,
                    out var drawX, out var drawY);

                var page = outputDoc.AddPage();
                page.Width = XUnit.FromPoint(pageWidth);
                page.Height = XUnit.FromPoint(pageHeight);

                using var graphics = XGraphics.FromPdfPage(page);
                graphics.DrawImage(image, drawX, drawY, drawWidth, drawHeight);
            }
        }

        if (outputDoc.PageCount > 0)
            outputDoc.Save(outputPath);
    }

    /// <summary>
    /// Calculates page dimensions and image draw position based on PageMode.
    /// </summary>
    private static void CalculatePageLayout(
        FileUtilityHub_WinUI.Core.Models.PdfPageMode mode, double imgWidth, double imgHeight,
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

}
