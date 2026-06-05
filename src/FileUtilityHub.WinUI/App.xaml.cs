using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using FileUtilityHub.Core.Contracts;
using FileUtilityHub_WinUI.Core.Services;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;
using FileUtilityHub_WinUI.Infrastructure.FileSystem;
using FileUtilityHub_WinUI.Features.ImageOptimizer;

namespace FileUtilityHub_WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Window MainWindow { get; private set; } = null!;
    
    /// <summary>
    /// Gets the current <see cref="App"/> instance in use
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; }
    
    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<FfmpegLocator>();
        services.AddSingleton<FfmpegRunner>();
        services.AddSingleton<OutputManager>();

        // Core Services
        services.AddSingleton<AppStatusService>();
        services.AddSingleton<FileScanService>();
        services.AddSingleton<ImageConvertService>();
        services.AddSingleton<IFeatureHandoffService, FeatureHandoffService>();

        // ViewModels
        services.AddTransient<ImageOptimizerViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Check FFmpeg availability on startup
        var locator = Services.GetRequiredService<FfmpegLocator>();
        var statusService = Services.GetRequiredService<AppStatusService>();
        statusService.IsFfmpegMissing = !locator.IsFfmpegAvailable();
    }
}
