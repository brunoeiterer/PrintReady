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
using System.Threading.Tasks;

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

    public async Task OnAddPicturesAsync(object sender, RoutedEventArgs e) => await Gallery.AddPicturesAsync();
}