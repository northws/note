using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;

namespace HandwrittenNotes.Services;

public class ImageImportService
{
    private static readonly string ImageStorageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HandwrittenNotes", "Images");

    public ImageImportService()
    {
        Directory.CreateDirectory(ImageStorageFolder);
    }

    public async Task<(string? ImagePath, double Width, double Height)> PickAndImportImageAsync()
    {
        var imageFile = await PickImageFileAsync();
        if (imageFile == null)
        {
            return (null, 0, 0);
        }

        var localFileName = $"{Guid.NewGuid()}_{imageFile.Name}";
        await imageFile.CopyAsync(
            await StorageFolder.GetFolderFromPathAsync(ImageStorageFolder),
            localFileName,
            NameCollisionOption.ReplaceExisting);

        var (width, height) = await ReadImageSizeAsync(imageFile);
        return (Path.Combine(ImageStorageFolder, localFileName), width, height);
    }

    private static async Task<StorageFile?> PickImageFileAsync()
    {
        var openPicker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        openPicker.FileTypeFilter.Add(".png");
        openPicker.FileTypeFilter.Add(".jpg");
        openPicker.FileTypeFilter.Add(".jpeg");
        openPicker.FileTypeFilter.Add(".bmp");
        openPicker.FileTypeFilter.Add(".webp");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance!);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

        return await openPicker.PickSingleFileAsync();
    }

    private static async Task<(double Width, double Height)> ReadImageSizeAsync(StorageFile file)
    {
        try
        {
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            return (decoder.PixelWidth, decoder.PixelHeight);
        }
        catch
        {
            return (0, 0);
        }
    }
}
