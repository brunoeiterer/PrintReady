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

namespace PrintReady;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<Border> Images = [];

    public MainWindow()
    {
        InitializeComponent();

        GalleryGrid.SelectionChanged += OnSelectionChange;
        GalleryGrid.ItemsSource = Images;

        ((OverlappedPresenter)AppWindow.Presenter).Maximize();
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
        var source = ((GalleryGrid.SelectedItem as Border)?.Child as Image)?.Source as BitmapImage;
        Preview.ImagePath = source?.UriSource.LocalPath;
    }

    public async void OnAddPictures(object sender, RoutedEventArgs e)
    {
        var senderButton = (Button)sender;
        senderButton.IsEnabled = false;

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

        senderButton.IsEnabled = true;
    }
}
