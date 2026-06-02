using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FileUtilityHub_WinUI.Infrastructure.Ffmpeg;

public class FfmpegRunner
{
    private readonly FfmpegLocator _locator;

    public FfmpegRunner(FfmpegLocator locator)
    {
        _locator = locator;
    }

    public async Task<bool> RunCommandAsync(string arguments)
    {
        var exe = _locator.LocateFfmpeg();
        var tcs = new TaskCompletionSource<bool>();
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(process.ExitCode == 0);
            process.Dispose();
        };

        process.Start();
        return await tcs.Task;
    }
}
