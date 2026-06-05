namespace FileUtilityHub_WinUI.Core.Models;

/// <summary>
/// Domain-level warning severity. Decoupled from WinUI InfoBarSeverity.
/// UI layer maps this to InfoBarSeverity.
/// </summary>
public enum WarningSeverityLevel
{
    Informational,
    Success,
    Warning,
    Error
}
