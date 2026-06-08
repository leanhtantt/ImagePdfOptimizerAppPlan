using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FileUtilityHub_WinUI.Core.Services;

public class PdfRenderService
{
    /// <summary>
    /// Renders a PDF file into a sequence of JPEG images.
    /// Returns a list of paths to the generated JPEG files.
    /// </summary>
    public async Task<List<string>> RenderPdfToJpegsAsync(
        string pdfFilePath, 
        string outputFolder, 
        int dpi, 
        int jpegQuality, 
        bool isGrayscale,
        CancellationToken cancellationToken)
    {
        var outputPaths = new List<string>();
        StorageFile pdfFile = await StorageFile.GetFileFromPathAsync(pdfFilePath);
        
        // Load the PDF Document
        PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(pdfFile);

        for (uint i = 0; i < pdfDoc.PageCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using PdfPage page = pdfDoc.GetPage(i);
            
            // Calculate dimensions based on DPI
            // PDF logical size is 72 DPI. 
            // So pixel dimension = logical size * (target DPI / 72.0)
            var destWidth = (uint)(page.Size.Width * (dpi / 72.0));
            var destHeight = (uint)(page.Size.Height * (dpi / 72.0));

            var renderOptions = new PdfPageRenderOptions
            {
                DestinationWidth = destWidth,
                DestinationHeight = destHeight,
                // If grayscale, we'll let it render normally then perhaps let FFmpeg or PdfBuilder handle color space later
                // Unfortunately WinRT PdfRenderOptions doesn't have a direct grayscale switch,
                // so we just output standard JPEG and handle Grayscale via our encoder or PDF builder.
            };

            // Using InMemoryRandomAccessStream for the rendered image
            using var stream = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(stream, renderOptions);
            await stream.FlushAsync();
            stream.Seek(0);

            // Re-encode to JPEG with specific quality
            // We read the BMP/PNG rendered by WinRT and re-encode it to our target JPEG quality
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            string tempJpegPath = Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(pdfFilePath)}_page_{i + 1}_{Guid.NewGuid():N}.jpg");
            var folder = await StorageFolder.GetFolderFromPathAsync(outputFolder);
            StorageFile outputFile = await folder.CreateFileAsync(
                Path.GetFileName(tempJpegPath),
                CreationCollisionOption.ReplaceExisting);

            using (var outStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoderId = BitmapEncoder.JpegEncoderId;
                
                // JPEG Quality (0.0 to 1.0)
                var qualityValue = new BitmapTypedValue(jpegQuality / 100.0, Windows.Foundation.PropertyType.Single);
                var propertySet = new[] { new KeyValuePair<string, BitmapTypedValue>("ImageQuality", qualityValue) };

                var encoder = await BitmapEncoder.CreateAsync(encoderId, outStream, propertySet);
                
                // Set pixel format for grayscale if needed, but JpegEncoder in WinRT doesn't support Grayscale natively (only Gray8 in some limited cases).
                // Safest to write RGB and let PDF builder embed it as grayscale if required.
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
            }

            outputPaths.Add(tempJpegPath);
        }

        return outputPaths;
    }
}
