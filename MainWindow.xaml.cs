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
using Microsoft.UI;
using System.Collections.Specialized;

namespace PrintReady;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<Border> Images = [];
    private readonly HashSet<string> ImagePathsAdded = [];
    private readonly List<GalleryImage> Items = [];
    private int GallerySelectedIndex { get; set; } = -1;

    public MainWindow()
    {
        InitializeComponent();

        GalleryGrid.SelectionChanged += OnSelectionChange;
        GalleryGrid.ItemsSource = Images;

        Images.CollectionChanged += Images_CollectionChanged;

        var addImagesBorder = new Border()
        {
            Child = new FontIcon()
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\ue710",
                FontSize = 64,
                Width = 150,
                Height = 150,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            }
        };

        Images.Add(addImagesBorder);
        Items.Add(new GalleryImage(150, 150));

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

            var imageSource = new BitmapImage(new Uri(imagePath));
            var imageData = System.Drawing.Image.FromFile(imagePath);
            imageData.FixOrientation();

            var scaleFactor = 150d / imageData.Height;

            var image = new Microsoft.UI.Xaml.Controls.Image
            {
                Source = imageSource,
                Width = imageData.Width * scaleFactor,
                Height = 150,
                Stretch = Stretch.Fill
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = image,
            };

            Images.Add(border);
            Items.Add(new GalleryImage(imageData.Width, imageData.Height));
        }

        var rows = ComputeLayout(Items, 150, GalleryGrid.ActualWidth - 20);

        var index = 0;
        foreach (var row in rows)
        {
            for(var i = 0; i < row.Count; i++)
            {
                if(Images[index].Child is Microsoft.UI.Xaml.Controls.Image image)
                {
                    image.Width = row[i].ScaledWidth;
                    image.Height = row[i].ScaledHeight;
                }
                else if (Images[index].Child is FontIcon icon)
                {
                    icon.Width = row[i].ScaledWidth;
                    icon.Height = row[i].ScaledHeight;
                }

                index++;
            }
        }

        RefreshGallery();
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

        if(outputFolder == null)
        {
            return;
        }

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

    public List<List<GalleryImage>> ComputeLayout(List<GalleryImage> images, double targetRowHeight, double containerWidth)
    {
        List<List<GalleryImage>> rows = new();
        List<GalleryImage> currentRow = new();
        double currentRowWidth = 0;

        foreach (var image in images)
        {
            double scaledWidth = image.AspectRatio * targetRowHeight;
            currentRowWidth += scaledWidth;
            currentRow.Add(image);

            if (currentRowWidth >= containerWidth * 0.9)
            {
                rows.Add(AdjustRow(currentRow, currentRowWidth, targetRowHeight, containerWidth));
                currentRow = new List<GalleryImage>();
                currentRowWidth = 0;
            }
        }

        if (currentRow.Count > 0)
        {
            rows.Add(AdjustLastRow(currentRow, currentRowWidth, targetRowHeight, containerWidth));
        }

        return rows;
    }

    private List<GalleryImage> AdjustRow(List<GalleryImage> row, double rowWidth, double targetRowHeight, double containerWidth)
    {
        double scaleFactor = containerWidth / rowWidth;
        foreach (var image in row)
        {
            image.ScaledWidth = image.AspectRatio * targetRowHeight * scaleFactor;
            image.ScaledHeight = targetRowHeight * scaleFactor;
        }
        return row;
    }

    private List<GalleryImage> AdjustLastRow(List<GalleryImage> row, double rowWidth, double targetRowHeight, double containerWidth)
    {
        double minRowWidth = containerWidth * 0.6;
        if (rowWidth < minRowWidth)
        {
            foreach (var image in row)
            {
                image.ScaledWidth = image.AspectRatio * targetRowHeight;
                image.ScaledHeight = targetRowHeight;
            }
        }
        else
        {
            row = AdjustRow(row, rowWidth, targetRowHeight, containerWidth);
        }
        return row;
    }

    private void Images_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for(var i = 0; i < Images.Count; i++)
        {
            var container = (GridViewItem)GalleryGrid.ItemContainerGenerator.ContainerFromIndex(i);
            if (container != null && Images[i].Child is Microsoft.UI.Xaml.Controls.Image image)
            {
                VariableSizedWrapGrid.SetColumnSpan(container, (int)image.Width + 1);
            }
            else if(container != null && Images[i].Child is FontIcon icon)
            {
                VariableSizedWrapGrid.SetColumnSpan(container, (int)icon.Width + 1);
            }
        }
    }

    public void OnGalleryLoaded(object sender, RoutedEventArgs e)
    {
        RefreshGallery();
    }

    private void RefreshGallery()
    {
        Images.Add(new Border());
        Images.RemoveAt(Images.Count - 1);
    }
}

public class GalleryImage
{
    public double AspectRatio { get; set; }
    public double ScaledWidth { get; set; }
    public double ScaledHeight { get; set; }

    public GalleryImage(double width, double height)
    {
        AspectRatio = width / height;
        ScaledWidth = width;
        ScaledHeight = height;
    }

    public override string ToString() => $"{AspectRatio} {ScaledWidth} {ScaledHeight}";
}