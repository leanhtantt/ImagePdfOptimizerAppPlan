using System;

namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Configuration for OCR requests.
/// URL is read from environment variable OCR_API_URL, fallback to default.
/// </summary>
public sealed class DocumentOcrConfig
{
    private const string DefaultOcrApiUrl = "http://100.91.163.48:18080";
    private const string EnvVarName = "OCR_API_URL";

    /// <summary>
    /// Base URL of the OCR API server.
    /// Resolved from environment variable OCR_API_URL, or default Tailscale address.
    /// </summary>
    public string OcrServerUrl { get; init; } = ResolveServerUrl();

    /// <summary>
    /// Timeout per file. Default 2 hours as OCR can be very slow for large documents.
    /// </summary>
    public TimeSpan TimeoutPerFile { get; init; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Remove images from DOCX output. Fixed true in MVP.
    /// </summary>
    public bool RemoveImages { get; init; } = true;

    /// <summary>
    /// Remove seals from DOCX output. Fixed true in MVP.
    /// </summary>
    public bool RemoveSeals { get; init; } = true;

    /// <summary>
    /// Auto-switch to landscape A4 for wide tables. Fixed true in MVP.
    /// </summary>
    public bool UseWidePageForWideTables { get; init; } = true;

    private static string ResolveServerUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable(EnvVarName);
        return string.IsNullOrWhiteSpace(envUrl) ? DefaultOcrApiUrl : envUrl.TrimEnd('/');
    }
}

/// <summary>
/// Status of the OCR server connection.
/// </summary>
public enum OcrServerStatus
{
    Unknown,
    Checking,
    Ready,
    Unreachable,
    Busy
}
