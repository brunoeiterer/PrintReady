using System.Drawing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PrintReady.Extensions;

namespace PrintReady.Controls;

public sealed partial class Preview : UserControl
{
    private string? _imagePath;
    public string? ImagePath 
    { 
        get => _imagePath; 
        set {
            _imagePath = value;
            DisplayPreview();
        } 
    }

    public Preview()
    {
        InitializeComponent();
    }

    private async void DisplayPreview()
    {
        if (ImagePath == null)
        {
            return;
        }

        var image = System.Drawing.Image.FromFile(ImagePath);
        image.FixOrientation();

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

        var resizedImageSource = await image.ToPrintReadyImage(resizedWidth, resizedHeight, Color.White).ToImageSourceAsync();

        var resizedImage = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = resizedImageSource,
            Width = resizedWidth,
            Height = resizedHeight,
            Stretch = Stretch.None
        };

        PreviewImageBorder.Child = resizedImage;
    }
}
