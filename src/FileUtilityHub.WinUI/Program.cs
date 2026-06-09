using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace FileUtilityHub_WinUI;

public static class Program
{
    private const string MainInstanceKey = "FileUtilityHub.Main";

    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var currentInstance = AppInstance.GetCurrent();
        var mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);

        if (!mainInstance.IsCurrent)
        {
            mainInstance.RedirectActivationToAsync(currentInstance.GetActivatedEventArgs())
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return;
        }

        mainInstance.Activated += (_, _) => ActivateMainWindow();

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static void ActivateMainWindow()
    {
        var window = App.MainWindow;
        if (window == null)
        {
            return;
        }

        window.DispatcherQueue.TryEnqueue(window.Activate);
    }
}
