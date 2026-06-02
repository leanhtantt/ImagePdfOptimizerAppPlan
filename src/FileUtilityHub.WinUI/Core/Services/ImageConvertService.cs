using System;
using System.IO;
using System.Threading.Tasks;
using FileUtilityHub.Core.Models;
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

        var vfScale = config.MaxLongEdge > 0 
            ? $"-vf \"scale='if(gt(iw,ih),min(iw,{config.MaxLongEdge}),-2)':'if(gt(iw,ih),-2,min(ih,{config.MaxLongEdge}))'\" " 
            : "";

        var args = $"-y -i \"{item.SourcePath}\" -frames:v 1 {vfScale}-c:v libaom-av1 -crf {config.AvifCrf} -cpu-used {config.AvifCpuUsed} -pix_fmt yuv420p \"{outFile}\"";

        item.Status = ProcessingStatus.Processing;
        var success = await _runner.RunCommandAsync(args);
        
        if (success && File.Exists(outFile))
        {
            item.OutputPath = outFile;
            item.OutputSizeBytes = new FileInfo(outFile).Length;
            if (config.SkipIfOutputLarger && item.OutputSizeBytes > item.OriginalSizeBytes)
            {
                item.Status = ProcessingStatus.Warning;
                item.Warning = "Output larger than original";
            }
            else
            {
                item.Status = ProcessingStatus.Success;
            }
            return true;
        }

        item.Status = ProcessingStatus.Error;
        item.ErrorMessage = "Conversion failed";
        return false;
    }
}
