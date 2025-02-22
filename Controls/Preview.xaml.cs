using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

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
    private const int PropertyTagOrientation = 0x112;

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
        image = FixImageOrientation(image);

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

        var resizedImageSource = await ResizeImage(image, resizedWidth, resizedHeight, Color.White);

        var resizedImage = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = resizedImageSource,
            Width = resizedWidth,
            Height = resizedHeight,
            Stretch = Stretch.None
        };

        PreviewImageBorder.Child = resizedImage;
    }

    public static async Task<ImageSource> ResizeImage(System.Drawing.Image originalImage, int width, int height, Color borderColor)
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
        graphics.Clear(borderColor);
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

    private static System.Drawing.Image FixImageOrientation(System.Drawing.Image image)
    {
        var propertyTagOrientationValue = image.GetPropertyItem(PropertyTagOrientation)?.Value;
        if(propertyTagOrientationValue == null)
        {
            return image;
        }

        int orientation = BitConverter.ToUInt16(propertyTagOrientationValue, 0);
        if (orientation == 3)
        {
            image.RotateFlip(RotateFlipType.Rotate180FlipNone);
        }
        else if (orientation == 6)
        {
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }
        else if (orientation == 8)
        {
            image.RotateFlip(RotateFlipType.Rotate270FlipNone);
        }

        image.RemovePropertyItem(PropertyTagOrientation);

        return image;
    }
}
