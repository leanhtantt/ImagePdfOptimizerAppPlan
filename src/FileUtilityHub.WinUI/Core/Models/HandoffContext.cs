using System;
using System.Collections.Generic;

namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Handoff contract: batch of files from one feature to another.
/// Ref: doc 09 section 7.
/// Used when Image Optimizer sends files to File Merge / PDF Builder.
/// </summary>
public sealed class FileBatchContext
{
    public string BatchId { get; init; } = Guid.NewGuid().ToString();
    public string SourceFeature { get; init; } = string.Empty;
    public string SourceFolder { get; init; } = string.Empty;
    public IReadOnlyList<string> Files { get; init; } = [];
    public string? OutputFolder { get; init; }
    public IReadOnlyList<string>? SuggestedOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}

/// <summary>
/// Handoff contract: a PDF job from one feature to another.
/// Ref: doc 09 section 7.
/// Used when File Merge sends PDF to PDF Compressor.
/// </summary>
public sealed class PdfJobContext
{
    public string JobId { get; init; } = Guid.NewGuid().ToString();
    public string SourceFeature { get; init; } = string.Empty;
    public string PdfPath { get; init; } = string.Empty;
    public string? OutputFolder { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}
