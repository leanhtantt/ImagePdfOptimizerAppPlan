using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// Orchestrates the OCR workflow for a single file:
/// Upload → OCR → Build DOCX → Save output.
/// </summary>
public sealed class DocumentOcrWorkflowService
{
    private readonly OcrApiClient _apiClient;
    private readonly OcrOutputManager _outputManager;

    public DocumentOcrWorkflowService(OcrApiClient apiClient, OcrOutputManager outputManager)
    {
        _apiClient = apiClient;
        _outputManager = outputManager;
    }

    /// <summary>
    /// Process a single OCR item: upload to server, receive DOCX, save to disk.
    /// Updates item status throughout the process.
    /// </summary>
    public async Task<bool> ProcessFileAsync(
        DocumentOcrItem item, DocumentOcrConfig config, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            item.Status = OcrProcessingStatus.Cancelled;
            return false;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate source file exists
            if (!File.Exists(item.SourcePath))
            {
                item.Status = OcrProcessingStatus.Error;
                item.ErrorMessage = "File nguồn không tồn tại.";
                return false;
            }

            // Step 1: Upload
            item.Status = OcrProcessingStatus.Uploading;

            // Step 2: Processing (server does OCR + DOCX building)
            item.Status = OcrProcessingStatus.Processing;

            using var result = await _apiClient.ConvertToDocxAsync(item.SourcePath, config, ct);

            if (ct.IsCancellationRequested)
            {
                item.Status = OcrProcessingStatus.Cancelled;
                return false;
            }

            if (!result.IsSuccess)
            {
                item.Status = OcrProcessingStatus.Error;
                item.ErrorMessage = result.ErrorMessage ?? "Lỗi không xác định từ OCR server.";
                return false;
            }

            // Step 3: Save DOCX
            item.Status = OcrProcessingStatus.BuildingDocx;

            var outputPath = _outputManager.GetOutputPath(item.SourcePath);
            await _outputManager.SaveDocxAsync(result.DocxStream!, outputPath);

            // Success
            var fi = new FileInfo(outputPath);
            item.OutputPath = outputPath;
            item.OutputSizeBytes = fi.Length;
            item.Status = OcrProcessingStatus.Success;

            return true;
        }
        catch (OperationCanceledException)
        {
            item.Status = OcrProcessingStatus.Cancelled;
            return false;
        }
        catch (Exception ex)
        {
            item.Status = OcrProcessingStatus.Error;
            item.ErrorMessage = $"Lỗi: {ex.Message}";
            return false;
        }
        finally
        {
            stopwatch.Stop();
            item.ElapsedTime = stopwatch.Elapsed;
        }
    }
}
