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

namespace PrintReady;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<Border> Images = [];
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
                FontFamily = new FontFamily("Segoe Fluent Icons"),
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

        if(e is ItemClickEventArgs args && args.ClickedItem == Images[0])
        {
            await AddPictures();
        }

        GalleryGrid.IsItemClickEnabled = true;
    }

    private void LoadImages(IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            var image = new Image
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
        if(GalleryGrid.SelectedIndex == 0)
        {
            GalleryGrid.SelectedIndex = GallerySelectedIndex;
        }
        else
        {
            GallerySelectedIndex = GalleryGrid.SelectedIndex;

            var source = ((GalleryGrid.SelectedItem as Border)?.Child as Image)?.Source as BitmapImage;
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
}
