using HandwrittenNotes.Models;
using HandwrittenNotes.ViewModels;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace HandwrittenNotes.Services;

public class PdfImportService
{
    private static readonly string PdfStorageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HandwrittenNotes", "Pdfs");

    private static readonly string ImageCacheFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HandwrittenNotes", "PdfCache");

    public PdfImportService()
    {
        Directory.CreateDirectory(PdfStorageFolder);
        Directory.CreateDirectory(ImageCacheFolder);
    }

    public async Task<(bool Success, string? Error, Notebook? Notebook, bool NeedPassword)> ImportPdfAsync(MainViewModel viewModel, string? password = null)
    {
        try
        {
            var file = await PickPdfFileAsync();
            if (file == null) return (false, null, null, false);

            return await ImportPdfFromStorageFileAsync(file, viewModel, password);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null, false);
        }
    }

    public async Task<(bool Success, string? Error, Notebook? Notebook, bool NeedPassword)> ImportPdfFromStorageFileAsync(
        StorageFile file, MainViewModel viewModel, string? password = null)
    {
        try
        {
            PdfDocument pdfDoc;
            try
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    pdfDoc = await PdfDocument.LoadFromFileAsync(file, password);
                }
                else
                {
                    pdfDoc = await PdfDocument.LoadFromFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
                {
                    var error = string.IsNullOrWhiteSpace(password)
                        ? "PDF需要密码才能打开"
                        : "密码错误或PDF无法解密";
                    return (false, error, null, true);
                }
                throw;
            }

            var notebook = new Notebook
            {
                Name = $"PDF: {file.DisplayName}",
                Pages = [],
                IsPdfNotebook = true
            };

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using var pdfPage = pdfDoc.GetPage(i);
                var pageDimensions = pdfPage.Dimensions;
                
                var dpi = 150u;
                var scaleFactor = dpi / 96.0;
                
                var renderOptions = new PdfPageRenderOptions
                {
                    DestinationWidth = (uint)(pageDimensions.MediaBox.Width * scaleFactor),
                    DestinationHeight = (uint)(pageDimensions.MediaBox.Height * scaleFactor)
                };

                using var stream = new InMemoryRandomAccessStream();
                await pdfPage.RenderToStreamAsync(stream, renderOptions);
                
                var imagePath = Path.Combine(ImageCacheFolder, $"{notebook.Id}_{i}.png");
                
                using var fileStream = File.Create(imagePath);
                using var inputStream = stream.GetInputStreamAt(0);
                using var dataReader = new DataReader(inputStream);
                await dataReader.LoadAsync((uint)stream.Size);
                var buffer = new byte[stream.Size];
                dataReader.ReadBytes(buffer);
                await fileStream.WriteAsync(buffer, 0, buffer.Length);

                var notePage = new NotePage
                {
                    PageIndex = (int)i,
                    Width = renderOptions.DestinationWidth,
                    Height = renderOptions.DestinationHeight,
                    BackgroundImagePath = imagePath
                };

                notebook.Pages.Add(notePage);
            }

            if (notebook.Pages.Count > 0)
            {
                var firstPage = notebook.Pages[0];
                notebook.PageWidth = firstPage.Width;
                notebook.PageHeight = firstPage.Height;
            }

            var localPdfPath = await CopyPdfToLocalAsync(file);
            notebook.PdfFilePath = localPdfPath;

            viewModel.Notebooks.Insert(0, notebook);
            viewModel.SelectNotebook(notebook);
            await viewModel.SaveCurrentNotebookAsync();

            return (true, null, notebook, false);
        }
        catch (Exception ex)
        {
            return (false, $"导入失败: {ex.Message}", null, false);
        }
    }

    private async Task<string> CopyPdfToLocalAsync(StorageFile file)
    {
        var localFileName = $"{Guid.NewGuid()}_{file.Name}";
        var localPath = Path.Combine(PdfStorageFolder, localFileName);
        await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(PdfStorageFolder), localFileName, NameCollisionOption.ReplaceExisting);
        return localPath;
    }

    public static async Task<StorageFile?> PickPdfFileAsync()
    {
        var openPicker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };

        openPicker.FileTypeFilter.Add(".pdf");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance!);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

        return await openPicker.PickSingleFileAsync();
    }
}
