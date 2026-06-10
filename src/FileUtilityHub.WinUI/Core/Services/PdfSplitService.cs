using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Splits a single PDF (or DOCX→PDF) into individual page images.
/// Orchestrates PdfRenderService for rendering and FFmpeg for format conversion.
/// </summary>
public class PdfSplitService
{
    private readonly PdfRenderService _pdfRenderService;
    private readonly FfmpegRunner _ffmpegRunner;
    private readonly OfficeConvertService _officeConvertService;

    public PdfSplitService(
        PdfRenderService pdfRenderService,
        FfmpegRunner ffmpegRunner,
        OfficeConvertService officeConvertService)
    {
        _pdfRenderService = pdfRenderService;
        _ffmpegRunner = ffmpegRunner;
        _officeConvertService = officeConvertService;
    }

    /// <summary>
    /// Splits a PDF/DOCX file into page images.
    /// Returns the output folder path and number of extracted images.
    /// </summary>
    public async Task<(string outputFolder, int extractedCount)> SplitAsync(
        PdfSplitItem item,
        PdfSplitConfig config,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        string pdfPath = item.SourcePath;
        string? tempPdfPath = null;

        try
        {
            // Step 1: If DOCX, convert to temporary PDF first
            if (item.Format.Equals("docx", StringComparison.OrdinalIgnoreCase))
            {
                item.Status = ProcessingStatus.Processing;
                var tempFolder = Path.Combine(Path.GetTempPath(), $"pdfsplit_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempFolder);

                tempPdfPath = await _officeConvertService.ConvertToPdfAsync(
                    item.SourcePath, tempFolder, cancellationToken);

                if (string.IsNullOrEmpty(tempPdfPath) || !File.Exists(tempPdfPath))
                {
                    throw new InvalidOperationException(
                        "Không thể chuyển DOCX sang PDF. Kiểm tra công cụ office_to_pdf.");
                }

                pdfPath = tempPdfPath;
            }

            // Step 2: Create output folder next to source file
            string parentDir = Path.GetDirectoryName(item.SourcePath)!;
            string folderName = Path.GetFileNameWithoutExtension(item.SourcePath);
            string outputFolder = Path.Combine(parentDir, folderName);

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            item.OutputFolderPath = outputFolder;

            // Step 3: Render PDF pages to JPEG intermediates
            item.Status = ProcessingStatus.Processing;

            // Use a temp subfolder for intermediates when format != JPEG
            bool needsConvert = config.OutputFormat != SplitOutputFormat.Jpeg;
            string renderFolder = needsConvert
                ? Path.Combine(Path.GetTempPath(), $"pdfsplit_render_{Guid.NewGuid():N}")
                : outputFolder;

            if (needsConvert && !Directory.Exists(renderFolder))
                Directory.CreateDirectory(renderFolder);

            List<string> jpegPaths = await _pdfRenderService.RenderPdfToJpegsAsync(
                pdfPath, renderFolder,
                config.EffectiveDpi, config.JpegQuality,
                isGrayscale: false,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            int extractedCount = 0;

            if (needsConvert)
            {
                // Step 4: Convert JPEG intermediates to target format
                foreach (var jpegPath in jpegPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string pageBaseName = $"{folderName}_page_{extractedCount + 1}.{config.OutputExtension}";
                    string outputPath = Path.Combine(outputFolder, pageBaseName);

                    bool success = await ConvertImageAsync(
                        jpegPath, outputPath, config);

                    if (success && File.Exists(outputPath))
                    {
                        extractedCount++;
                        item.ExtractedCount = extractedCount;
                    }

                    // Clean up intermediate JPEG
                    try { File.Delete(jpegPath); } catch { }
                }

                // Clean up temp render folder
                try { Directory.Delete(renderFolder, true); } catch { }
            }
            else
            {
                // JPEG output — rename intermediates with clean names
                for (int i = 0; i < jpegPaths.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string cleanName = $"{folderName}_page_{i + 1}.jpg";
                    string cleanPath = Path.Combine(outputFolder, cleanName);

                    if (jpegPaths[i] != cleanPath)
                    {
                        if (File.Exists(cleanPath))
                            File.Delete(cleanPath);
                        File.Move(jpegPaths[i], cleanPath);
                    }

                    extractedCount++;
                    item.ExtractedCount = extractedCount;
                }
            }

            sw.Stop();
            item.ElapsedTime = sw.Elapsed;
            item.Status = ProcessingStatus.Success;

            return (outputFolder, extractedCount);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            item.ElapsedTime = sw.Elapsed;
            item.Status = ProcessingStatus.Skipped;
            item.ErrorMessage = "Đã hủy.";
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            item.ElapsedTime = sw.Elapsed;
            item.Status = ProcessingStatus.Error;
            item.ErrorMessage = ex.Message;
            return (string.Empty, 0);
        }
        finally
        {
            // Clean up temporary PDF from DOCX conversion
            if (tempPdfPath != null)
            {
                try { File.Delete(tempPdfPath); } catch { }
            }
        }
    }

    private async Task<bool> ConvertImageAsync(
        string inputPath, string outputPath, PdfSplitConfig config)
    {
        string args = config.OutputFormat switch
        {
            SplitOutputFormat.Png => FfmpegCommandBuilder.BuildPngConvertCommand(
                inputPath, outputPath),
            SplitOutputFormat.Avif => FfmpegCommandBuilder.BuildAvifConvertCommand(
                inputPath, outputPath, config.AvifCrf, cpuUsed: 6, maxLongEdge: 0),
            SplitOutputFormat.WebP => FfmpegCommandBuilder.BuildWebpConvertCommand(
                inputPath, outputPath, config.WebPQuality),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(args))
            return false;

        return await _ffmpegRunner.RunCommandAsync(args);
    }
}
