using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using PrintReady.Extensions;
using System.Drawing;
using System.IO;

namespace PrintReady;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ((OverlappedPresenter)AppWindow.Presenter).Maximize();
    }

    public void OnDragEnter(object sender, DragEventArgs e)
    {
        DragOverlay.Visibility = Visibility.Visible;
    }

    public void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    public void OnDragLeave(object sender, DragEventArgs e)
    {
        DragOverlay.Visibility = Visibility.Collapsed;
    }

    public async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var files = (await e.DataView.GetStorageItemsAsync()).Where(i => i.IsOfType(StorageItemTypes.File)).Cast<StorageFile>();
            var imageFiles = files.Where(f => f.ContentType == "image/png" || f.ContentType == "image/jpeg");
            var imagePaths = imageFiles.Select(f => f.Path);
            Gallery.LoadImages(imagePaths);
        }

        DragOverlay.Visibility = Visibility.Collapsed;
    }
    public async void OnExportButtonClickAsync(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

        var outputFolder = await picker.PickSingleFolderAsync();

        if(outputFolder == null)
        {
            return;
        }

        foreach(var imageBorder in Gallery.ImageBorders)
        {
            var source = (imageBorder.Child as Microsoft.UI.Xaml.Controls.Image)?.Source as BitmapImage;
            var path = source?.UriSource.LocalPath;

            if(path == null)
            {
                continue;
            }

            var image = System.Drawing.Image.FromFile(path);
            Bitmap printReadyImage;
            if(image.Width > image.Height)
            {
                printReadyImage = image.ToPrintReadyImage(1800, 1200, Color.White);
            }
            else
            {
                printReadyImage = image.ToPrintReadyImage(1200, 1800, Color.White);
            }

            var extension = Path.GetExtension(path);
            var filename = Path.GetFileNameWithoutExtension(path);
            printReadyImage.Save(Path.Combine(outputFolder.Path, $"{filename}_PrintReady.{extension}"));
        }
    }
}