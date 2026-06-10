using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;

namespace FileUtilityHub_WinUI.Core.Services;

/// <summary>
/// HTTP client for communicating with the OCR backend (LocalScanner).
/// Handles health checks and file upload for DOCX conversion.
/// </summary>
public sealed class OcrApiClient
{
    private readonly HttpClient _httpClient;

    public OcrApiClient()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Check OCR server health via GET /health.
    /// </summary>
    public async Task<OcrServerStatus> CheckHealthAsync(string serverUrl)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync($"{serverUrl}/health", cts.Token);

            if (response.IsSuccessStatusCode)
                return OcrServerStatus.Ready;

            if ((int)response.StatusCode == 503)
                return OcrServerStatus.Busy;

            return OcrServerStatus.Unreachable;
        }
        catch (TaskCanceledException)
        {
            return OcrServerStatus.Unreachable;
        }
        catch (HttpRequestException)
        {
            return OcrServerStatus.Unreachable;
        }
        catch
        {
            return OcrServerStatus.Unreachable;
        }
    }

    /// <summary>
    /// Upload a file to POST /api/convert-to-docx and return the DOCX stream.
    /// </summary>
    public async Task<OcrConvertResult> ConvertToDocxAsync(
        string filePath, DocumentOcrConfig config, CancellationToken ct)
    {
        var url = $"{config.OcrServerUrl}/api/convert-to-docx";

        using var content = new MultipartFormDataContent();

        // File content
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        // Form fields
        content.Add(new StringContent(config.RemoveImages.ToString().ToLower()), "removeImages");
        content.Add(new StringContent(config.RemoveSeals.ToString().ToLower()), "removeSeals");
        content.Add(new StringContent(config.UseWidePageForWideTables.ToString().ToLower()), "useWidePageForWideTables");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(config.TimeoutPerFile);

        try
        {
            var response = await _httpClient.PostAsync(url, content, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var docxStream = await response.Content.ReadAsStreamAsync(cts.Token);

                // Copy to MemoryStream so we own the data
                var memStream = new MemoryStream();
                await docxStream.CopyToAsync(memStream, cts.Token);
                memStream.Position = 0;

                return new OcrConvertResult
                {
                    IsSuccess = true,
                    DocxStream = memStream
                };
            }

            // Try parse error response
            var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
            return new OcrConvertResult
            {
                IsSuccess = false,
                ErrorMessage = ParseErrorMessage(errorBody, response.StatusCode)
            };
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            return new OcrConvertResult { IsSuccess = false, ErrorMessage = "Đã hủy bởi người dùng." };
        }
        catch (TaskCanceledException)
        {
            return new OcrConvertResult { IsSuccess = false, ErrorMessage = "Hết thời gian chờ OCR server phản hồi." };
        }
        catch (HttpRequestException ex)
        {
            return new OcrConvertResult { IsSuccess = false, ErrorMessage = $"Không kết nối được OCR server: {ex.Message}" };
        }
    }

    private static string ParseErrorMessage(string body, System.Net.HttpStatusCode statusCode)
    {
        // Try to extract "message" field from JSON error
        try
        {
            if (body.Contains("\"message\""))
            {
                var start = body.IndexOf("\"message\"") + "\"message\"".Length;
                start = body.IndexOf('"', start) + 1;
                var end = body.IndexOf('"', start);
                if (start > 0 && end > start)
                    return body[start..end];
            }
        }
        catch { /* Fallback below */ }

        return $"OCR server trả lỗi {(int)statusCode}: {statusCode}";
    }
}

/// <summary>
/// Result from an OCR convert-to-docx request.
/// </summary>
public sealed class OcrConvertResult : IDisposable
{
    public bool IsSuccess { get; init; }
    public MemoryStream? DocxStream { get; init; }
    public string? ErrorMessage { get; init; }

    public void Dispose()
    {
        DocxStream?.Dispose();
    }
}
