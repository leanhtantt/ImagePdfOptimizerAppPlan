using System;
using System.IO;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Infrastructure.Ffmpeg;
using FileUtilityHub_WinUI.Infrastructure.FileSystem;

namespace FileUtilityHub_WinUI.Core.Services;

public class ImageConvertService
{
    private readonly FfmpegRunner _runner;
    private readonly OutputManager _outputManager;

    public ImageConvertService(FfmpegRunner runner, OutputManager outputManager)
    {
        _runner = runner;
        _outputManager = outputManager;
    }

    public async Task<bool> ConvertToAvifAsync(ImageItem item, ImageConvertConfig config, string sourceFolder)
    {
        var outDir = _outputManager.CreateAvifOutputDirectory(sourceFolder);
        var outFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(item.FileName) + ".avif");

        // Use FfmpegCommandBuilder instead of inline string building
        var args = FfmpegCommandBuilder.BuildAvifConvertCommand(
            item.SourcePath, outFile, config.AvifCrf, config.AvifCpuUsed, config.MaxLongEdge);

        item.AvifCrfUsed = config.AvifCrf;
        item.MaxLongEdgeUsed = config.MaxLongEdge;
        item.Warning = null;
        item.ErrorMessage = null;
        item.Status = ProcessingStatus.Processing;
        var success = await _runner.RunCommandAsync(args);
        
        if (success && File.Exists(outFile))
        {
            item.OutputPath = outFile;
            item.OutputSizeBytes = new FileInfo(outFile).Length;
            if (config.SkipIfOutputLarger && item.OutputSizeBytes > item.OriginalSizeBytes)
            {
                item.Status = ProcessingStatus.Warning;
                item.Warning = "Output AVIF nặng hơn file gốc, cần xem lại CRF hoặc giữ file gốc.";
            }
            else
            {
                item.Status = ProcessingStatus.Success;
            }
            return true;
        }

        item.Status = ProcessingStatus.Error;
        item.ErrorMessage = "Nén AVIF thất bại. Kiểm tra FFmpeg, file input hoặc quyền ghi thư mục output.";
        return false;
    }
}
