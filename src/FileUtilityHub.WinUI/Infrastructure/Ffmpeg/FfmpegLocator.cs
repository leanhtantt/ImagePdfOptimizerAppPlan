using System;
using System.IO;

namespace FileUtilityHub_WinUI.Infrastructure.Ffmpeg;

public class FfmpegLocator
{
    public string LocateFfmpeg()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var bundledPath = Path.Combine(currentDir, "tools", "ffmpeg", "bin", "ffmpeg.exe");

        if (File.Exists(bundledPath))
            return bundledPath;

        var localFfmpeg = Path.Combine(currentDir, "ffmpeg.exe");
        if (File.Exists(localFfmpeg))
            return localFfmpeg;

        return "ffmpeg"; // Assume it's in PATH as fallback
    }

    public bool IsFfmpegAvailable()
    {
        try
        {
            var exe = LocateFfmpeg();
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
