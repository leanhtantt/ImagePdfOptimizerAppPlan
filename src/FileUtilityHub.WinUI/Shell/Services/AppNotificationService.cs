using System;
using FileUtilityHub_WinUI.Core.Contracts;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace FileUtilityHub_WinUI.Shell.Services;

/// <summary>
/// Shell-level implementation of native Windows toast notifications.
/// </summary>
public class AppNotificationService : INotificationService
{
    public void ShowToast(string title, string message, string? iconUri = null)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message);

            if (!string.IsNullOrEmpty(iconUri))
            {
                if (iconUri.StartsWith("ms-appx:///"))
                {
                    // For unpackaged WinUI 3 apps, ms-appx:/// won't work in AppNotifications.
                    // Resolve to an absolute path:
                    var relativePath = iconUri.Substring(11).Replace('/', '\\');
                    var absolutePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, relativePath);
                    iconUri = new Uri(absolutePath).ToString();
                }
                
                builder.SetAppLogoOverride(new Uri(iconUri), AppNotificationImageCrop.Default);
            }

            var notification = builder.BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show toast notification: {ex.Message}");
        }
    }
}
