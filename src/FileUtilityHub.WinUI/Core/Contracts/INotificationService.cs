using System;

namespace FileUtilityHub_WinUI.Core.Contracts;

public interface INotificationService
{
    /// <summary>
    /// Shows a native Windows toast notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content.</param>
    /// <param name="iconUri">Optional URI to an icon image (e.g. ms-appx:///Assets/AppIcon.ico).</param>
    void ShowToast(string title, string message, string? iconUri = null);
}
