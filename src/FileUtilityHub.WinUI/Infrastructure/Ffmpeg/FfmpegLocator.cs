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

        throw new FileNotFoundException(
            "Bản cài đặt thiếu FFmpeg bundled. Vui lòng cài lại hoặc cập nhật ứng dụng.",
            bundledPath);
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
            if (!process.WaitForExit(5000))
            {
                process.Kill(true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
