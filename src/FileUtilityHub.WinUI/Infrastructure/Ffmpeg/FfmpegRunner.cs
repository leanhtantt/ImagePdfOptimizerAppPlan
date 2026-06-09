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

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);

        return process.ExitCode == 0;
    }
}
