using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileUtilityHub_WinUI.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FileUtilityHub_WinUI.Core.Services;

public class PdfCompressorService
{
    private readonly PdfRenderService _pdfRenderService;

    public PdfCompressorService(PdfRenderService pdfRenderService)
    {
        _pdfRenderService = pdfRenderService;
    }

    /// <summary>
    /// Loads a PDF document and generates thumbnails for all pages.
    /// </summary>
    public async Task<PdfDocumentSession> LoadPdfAsync(string filePath, string tempFolder)
    {
        var session = new PdfDocumentSession
        {
            OriginalPath = filePath,
            CurrentVersionPath = filePath
        };

        StorageFile pdfFile = await StorageFile.GetFileFromPathAsync(filePath);
        Windows.Data.Pdf.PdfDocument pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(pdfFile);

        var folder = await StorageFolder.GetFolderFromPathAsync(tempFolder);

        for (uint i = 0; i < pdfDoc.PageCount; i++)
        {
            using var page = pdfDoc.GetPage(i);
            
            // Render a small thumbnail
            var thumbOptions = new PdfPageRenderOptions { DestinationWidth = 200 };
            using var thumbStream = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(thumbStream, thumbOptions);
            await thumbStream.FlushAsync();
            thumbStream.Seek(0);

            var thumbDecoder = await BitmapDecoder.CreateAsync(thumbStream);
            var thumbBitmap = await thumbDecoder.GetSoftwareBitmapAsync();

            string thumbName = $"thumb_page_{i + 1}_{Guid.NewGuid():N}.jpg";
            StorageFile thumbFile = await folder.CreateFileAsync(thumbName, CreationCollisionOption.ReplaceExisting);

            using (var outStream = await thumbFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream);
                encoder.SetSoftwareBitmap(thumbBitmap);
                await encoder.FlushAsync();
            }

            // Render a large preview
            var previewOptions = new PdfPageRenderOptions { DestinationWidth = 1000 };
            using var previewStream = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(previewStream, previewOptions);
            await previewStream.FlushAsync();
            previewStream.Seek(0);

            var previewDecoder = await BitmapDecoder.CreateAsync(previewStream);
            var previewBitmap = await previewDecoder.GetSoftwareBitmapAsync();

            string previewName = $"preview_page_{i + 1}_{Guid.NewGuid():N}.jpg";
            StorageFile previewFile = await folder.CreateFileAsync(previewName, CreationCollisionOption.ReplaceExisting);

            using (var outStream = await previewFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream);
                encoder.SetSoftwareBitmap(previewBitmap);
                await encoder.FlushAsync();
            }

            session.Pages.Add(new PdfPageItem
            {
                PageNumber = (int)(i + 1),
                ThumbnailPath = thumbFile.Path,
                PreviewImagePath = previewFile.Path,
                IsSelected = false
            });
        }

        return session;
    }

    /// <summary>
    /// Compresses the selected pages of the PDF.
    /// </summary>
    public async Task<PdfCompressionVersion> CompressPdfAsync(
        PdfDocumentSession session, 
        PdfCompressionSettings settings, 
        string outputFolder,
        Action<int, int, string> progressCallback,
        CancellationToken cancellationToken)
    {
        string inputPath = session.CurrentVersionPath;
        string baseName = Path.GetFileNameWithoutExtension(session.OriginalPath);
        string newName = $"{baseName}_{settings.Dpi}_{settings.JpegQuality}.pdf";
        string outputPath = Path.Combine(outputFolder, newName);

        int counter = 1;
        while (File.Exists(outputPath))
        {
            newName = $"{baseName}_{settings.Dpi}_{settings.JpegQuality}_{counter}.pdf";
            outputPath = Path.Combine(outputFolder, newName);
            counter++;
        }

        // Determine pages to compress
        var selectedPages = session.Pages.Where(p => p.IsSelected).Select(p => p.PageNumber).ToHashSet();
        if (selectedPages.Count == 0)
        {
            // If no pages selected, compress all
            selectedPages = session.Pages.Select(p => p.PageNumber).ToHashSet();
        }

        // We use PDFsharp to open the current PDF
        using var inputDoc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
        using var outputDoc = new PdfSharp.Pdf.PdfDocument();

        // Temporary folder for JPEGs
        string tempJpegFolder = Path.Combine(outputFolder, $"_temp_compress_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempJpegFolder);

        // Open the document once for rendering
        StorageFile pdfFileToRender = await StorageFile.GetFileFromPathAsync(inputPath);
        Windows.Data.Pdf.PdfDocument pdfDocToRender = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(pdfFileToRender);

        try
        {
            for (int i = 0; i < inputDoc.PageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int pageNumber = i + 1;

                progressCallback?.Invoke(i, inputDoc.PageCount, $"Đang xử lý trang {pageNumber}...");

                if (selectedPages.Contains(pageNumber))
                {
                    // Recompress: render page to JPEG using WinRT, then add to PDFsharp
                    string tempJpegPath = await RenderSinglePageToJpegAsync(pdfDocToRender, (uint)i, tempJpegFolder, settings.Dpi, settings.JpegQuality);
                    
                    var origPage = inputDoc.Pages[i];
                    var newPage = outputDoc.AddPage();
                    newPage.Width = origPage.Width;
                    newPage.Height = origPage.Height;
                    newPage.Orientation = origPage.Orientation;

                    using var gfx = XGraphics.FromPdfPage(newPage);
                    using var xImage = XImage.FromFile(tempJpegPath);
                    
                    // Draw image to fill the page
                    gfx.DrawImage(xImage, 0, 0, newPage.Width.Point, newPage.Height.Point);
                }
                else
                {
                    // Preserve: copy page directly
                    outputDoc.AddPage(inputDoc.Pages[i]);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            progressCallback?.Invoke(inputDoc.PageCount, inputDoc.PageCount, "Đang lưu PDF...");
            outputDoc.Save(outputPath);
        }
        finally
        {
            // Cleanup temp JPEGs
            try
            {
                if (Directory.Exists(tempJpegFolder))
                    Directory.Delete(tempJpegFolder, true);
            }
            catch { }
        }

        var fileInfo = new FileInfo(outputPath);
        var version = new PdfCompressionVersion
        {
            FilePath = outputPath,
            FileName = fileInfo.Name,
            FileSizeBytes = fileInfo.Length,
            CreatedAt = DateTime.Now,
            SettingsSummary = $"DPI: {settings.Dpi}, Q: {settings.JpegQuality}, Pages: {selectedPages.Count}/{inputDoc.PageCount}",
            CompressedPages = selectedPages.ToList()
        };

        return version;
    }

    private async Task<string> RenderSinglePageToJpegAsync(Windows.Data.Pdf.PdfDocument pdfDoc, uint pageIndex, string outputFolder, int dpi, int jpegQuality)
    {
        using var page = pdfDoc.GetPage(pageIndex);

        var destWidth = (uint)(page.Size.Width * (dpi / 72.0));
        var destHeight = (uint)(page.Size.Height * (dpi / 72.0));

        var renderOptions = new PdfPageRenderOptions
        {
            DestinationWidth = destWidth,
            DestinationHeight = destHeight
        };

        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, renderOptions);
        await stream.FlushAsync();
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        string tempJpegPath = Path.Combine(outputFolder, $"page_{pageIndex}_{Guid.NewGuid():N}.jpg");
        var folder = await StorageFolder.GetFolderFromPathAsync(outputFolder);
        StorageFile outputFile = await folder.CreateFileAsync(Path.GetFileName(tempJpegPath), CreationCollisionOption.ReplaceExisting);

        using (var outStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
        {
            var qualityValue = new BitmapTypedValue(jpegQuality / 100.0, Windows.Foundation.PropertyType.Single);
            var propertySet = new[] { new KeyValuePair<string, BitmapTypedValue>("ImageQuality", qualityValue) };

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream, propertySet);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
        }

        return outputFile.Path;
    }
}
