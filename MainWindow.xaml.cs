using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Threading.Tasks;
using Microsoft.UI;
using WinRT.Interop;
using PrintReady.Services;

namespace PrintReady;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ((OverlappedPresenter)AppWindow.Presenter).Maximize();

        if (AppWindowTitleBar.IsCustomizationSupported() is true)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(wndId);
            appWindow.SetIcon(@"Assets\LogoPrintReady.ico");
        }
    }

    public void OnDragEnter(object sender, DragEventArgs e)
    {
        Logger.Log("Drag Enter");
        DragOverlay.Visibility = Visibility.Visible;
    }

    public void OnDragOver(object sender, DragEventArgs e)
    {
        Logger.Log("Drag Over");
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    public void OnDragLeave(object sender, DragEventArgs e)
    {
        Logger.Log("Drag Leave");
        DragOverlay.Visibility = Visibility.Collapsed;
    }

    public async void OnDrop(object sender, DragEventArgs e)
    {
        Logger.Log("Drop");
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var files = (await e.DataView.GetStorageItemsAsync()).Where(i => i.IsOfType(StorageItemTypes.File)).Cast<StorageFile>();
            var imageFiles = files.Where(f => f.ContentType == "image/png" || f.ContentType == "image/jpeg");
            var imagePaths = imageFiles.Select(f => f.Path);
            await Gallery.LoadImages(imagePaths);
        }

        DragOverlay.Visibility = Visibility.Collapsed;
    }

    public async Task OnAddPicturesAsync(object sender, RoutedEventArgs e) => await Gallery.AddPicturesAsync();
}