using System.Collections.Generic;

namespace FileUtilityHub.Core.Models;

/// <summary>
/// Structured operation result. All services should return this instead of bool/string.
/// Ref: doc 07 section 8.
/// </summary>
public sealed class OperationResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public IReadOnlyList<AppWarning> Warnings { get; init; } = [];
    public AppError? Error { get; init; }

    public static OperationResult<T> Success(T value, IReadOnlyList<AppWarning>? warnings = null)
        => new() { Succeeded = true, Value = value, Warnings = warnings ?? [] };

    public static OperationResult<T> Failure(AppError error, IReadOnlyList<AppWarning>? warnings = null)
        => new() { Succeeded = false, Error = error, Warnings = warnings ?? [] };
}

/// <summary>
/// Structured warning with user-facing message and technical detail.
/// </summary>
public sealed class AppWarning
{
    public string Code { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public string? TechnicalDetail { get; init; }
    public string? RelatedFile { get; init; }
    public string? SuggestedAction { get; init; }
}

/// <summary>
/// Structured error with user-facing message and technical detail.
/// </summary>
public sealed class AppError
{
    public string Code { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public string? TechnicalDetail { get; init; }
    public string? RelatedFile { get; init; }
}
