using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileUtilityHub_WinUI.Core.Services;

public class OfficeConvertService
{
    private string _pythonScriptPath;

    public OfficeConvertService()
    {
        // Assume script is next to the executable in Tools/office folder
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _pythonScriptPath = Path.Combine(baseDir, "Tools", "office", "office_to_pdf.py");
    }

    /// <summary>
    /// Converts an Office document to PDF using a python headless script.
    /// Returns the path to the temporary PDF file.
    /// </summary>
    public async Task<string> ConvertToPdfAsync(string inputFilePath, string outputFolder, CancellationToken cancellationToken)
    {
        if (!File.Exists(_pythonScriptPath))
        {
            // For development, try to find it relative to the project if not in output dir
            var devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Tools", "office", "office_to_pdf.py"));
            if (File.Exists(devPath))
            {
                _pythonScriptPath = devPath;
            }
            else
            {
                throw new FileNotFoundException($"Cannot find python script at {_pythonScriptPath}. Ensure it is copied to the output directory.");
            }
        }

        var tcs = new TaskCompletionSource<string>();
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{_pythonScriptPath}\" \"{inputFilePath}\" \"{outputFolder}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process.EnableRaisingEvents = true;

        string lastOutput = string.Empty;
        string allErrors = string.Empty;

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lastOutput = e.Data.Trim();
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                allErrors += e.Data + Environment.NewLine;
            }
        };

        process.Exited += (sender, e) =>
        {
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(lastOutput) && File.Exists(lastOutput))
            {
                tcs.TrySetResult(lastOutput);
            }
            else
            {
                var errorMsg = string.IsNullOrWhiteSpace(allErrors) ? lastOutput : allErrors;
                tcs.TrySetException(new Exception($"Office to PDF conversion failed (ExitCode: {process.ExitCode}): {errorMsg}"));
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch { }
                tcs.TrySetCanceled();
            });

            return await tcs.Task;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new Exception("Cần Python + pywin32 để chuyển Office sang PDF. Không tìm thấy lệnh 'python' trong môi trường.");
        }
    }
}
