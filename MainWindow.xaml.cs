using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Microsoft.UI.Windowing;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using PrintReady.Extensions;
using System.Drawing;
using System.IO;

namespace PrintReady;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<Border> Images = [];
    private readonly HashSet<string> ImagePathsAdded = [];
    private int GallerySelectedIndex { get; set; } = -1;

    public MainWindow()
    {
        InitializeComponent();

        GalleryGrid.SelectionChanged += OnSelectionChange;
        GalleryGrid.ItemsSource = Images;

        var AddImagesBorder = new Border()
        {
            Child = new FontIcon()
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\ue710",
                FontSize = 64,
                Width = 200,
                Height = 200,
            }
        };

        Images.Add(AddImagesBorder);

        GalleryGrid.IsItemClickEnabled = true;
        GalleryGrid.ItemClick += OnGalleryItemClicked;

        ((OverlappedPresenter)AppWindow.Presenter).Maximize();
    }

    private async void OnGalleryItemClicked(object sender, RoutedEventArgs e)
    {
        GalleryGrid.IsItemClickEnabled = false;

        if (e is ItemClickEventArgs args && ReferenceEquals(args.ClickedItem, Images[0]))
        {
            await AddPictures();
        }

        GalleryGrid.IsItemClickEnabled = true;
    }

    private void LoadImages(IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            if (!ImagePathsAdded.Add(imagePath))
            {
                continue;
            }

            var image = new Microsoft.UI.Xaml.Controls.Image
            {
                Source = new BitmapImage(new Uri(imagePath)),
                Width = 200,
                Height = 200,
                Stretch = Stretch.Fill
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(5),
                Child = image,
            };

            Images.Add(border);
        }
    }

    private void OnSelectionChange(object sender, SelectionChangedEventArgs e)
    {
        if (GalleryGrid.SelectedIndex == 0)
        {
            GalleryGrid.SelectedIndex = GallerySelectedIndex;
        }
        else
        {
            GallerySelectedIndex = GalleryGrid.SelectedIndex;

            var source = ((GalleryGrid.SelectedItem as Border)?.Child as Microsoft.UI.Xaml.Controls.Image)?.Source as BitmapImage;
            Preview.ImagePath = source?.UriSource.LocalPath;
        }
    }

    public async Task AddPictures()
    {
        var picker = new FileOpenPicker();

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");

        var files = await picker.PickMultipleFilesAsync();
        LoadImages(files.Select(f => f.Path));
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
            var files = (await e.DataView.GetStorageItemsAsync()).Where(i => i.IsOfType(Windows.Storage.StorageItemTypes.File)).Cast<StorageFile>();
            var imageFiles = files.Where(f => f.ContentType == "image/png" || f.ContentType == "image/jpeg");
            var imagePaths = imageFiles.Select(f => f.Path);
            LoadImages(imagePaths);
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

        foreach(var imageBorder in Images)
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

    public void OnGalleryLoaded(object sender, RoutedEventArgs e)
    {
        GalleryGrid.HorizontalAlignment = HorizontalAlignment.Center;
    }
}
