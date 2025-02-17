using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
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

        GalleryGrid.SelectionChanged += DisplayPreview;
        GalleryGrid.ItemsSource = Images;

        ((OverlappedPresenter)AppWindow.Presenter).Maximize();
    }

    private void LoadImages(IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            var image = new Microsoft.UI.Xaml.Controls.Image
            {
                Source = new BitmapImage(new Uri(imagePath)),
                Width = 200,
                Height = 200,
                Stretch = Stretch.UniformToFill
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

    private async void DisplayPreview(object sender, SelectionChangedEventArgs e)
    {
        PreviewGrid.Children.Clear();

        var source = ((GalleryGrid.SelectedItem as Border)?.Child as Microsoft.UI.Xaml.Controls.Image)?.Source as BitmapImage;

        if(source == null)
        {
            return;
        }

        var originalHeight = source.PixelHeight;
        var originalWidth = source.PixelWidth;

        var image = System.Drawing.Image.FromFile(source.UriSource.LocalPath);
        int resizedWidth;
        int resizedHeight;
        if(image.Height > image.Width)
        {
            resizedHeight = 500;
            resizedWidth = (int)(500 / 1.5);
        }
        else
        {
            resizedHeight = (int)(500 / 1.5);
            resizedWidth = 500;
        }

        var resizedImageWithoutBorderSource = await ResizeImage(image, resizedWidth, resizedHeight, false);
        var resizedImageSource = await ResizeImage(image, resizedWidth, resizedHeight);

        var originalImage = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = resizedImageWithoutBorderSource,
            Width = resizedWidth,
            Height = resizedHeight,
            Stretch = Stretch.None
        };

        var originalImageBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Child = originalImage,
        };

        var resizedImage = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = resizedImageSource,
            Width = resizedWidth,
            Height = resizedHeight,
            Stretch = Stretch.None
        };

        var resizedImageBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(5),
            Child = resizedImage,
        };

        PreviewGrid.Children.Add(originalImageBorder);

        PreviewGrid.Children.Add(resizedImageBorder);
        Grid.SetColumn(resizedImageBorder, 1);
    }

    public static async Task<ImageSource> ResizeImage(System.Drawing.Image originalImage, int width, int height, bool addBorder = true)
    {
        var scaleX = (float)width / originalImage.Width;
        var scaleY = (float)height / originalImage.Height;
        var scale = Math.Min(scaleX, scaleY);

        var newWidth = (int)(originalImage.Width * scale);
        var newHeight = (int)(originalImage.Height * scale);

        var offsetX = (width - newWidth) / 2;
        var offsetY = (height - newHeight) / 2;

        Bitmap borderedImage = new(width, height);

        using var graphics = Graphics.FromImage(borderedImage);

        if(addBorder)
        {
            graphics.Clear(Color.Black);
        }
        
        graphics.DrawImage(originalImage, new Rectangle(offsetX, offsetY, newWidth, newHeight));
        return await GetWinUI3BitmapSourceFromGdiBitmap(borderedImage);
    }


    public static async Task<ImageSource> GetWinUI3BitmapSourceFromGdiBitmap(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
        var bytes = new byte[data.Stride * data.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        bitmap.UnlockBits(data);

        var softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            bitmap.Width,
            bitmap.Height,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(bytes.AsBuffer());

        var source = new SoftwareBitmapSource();
        await source.SetBitmapAsync(softwareBitmap);
        return source;
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
