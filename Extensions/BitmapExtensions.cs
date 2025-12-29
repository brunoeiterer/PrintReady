using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics.Imaging;
using System.IO;

namespace PrintReady.Extensions
{
    public static class BitmapExtensions
    {
        public static async Task<ImageSource> ToImageSourceAsync(this Bitmap bitmap)
        {
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            bitmap.Save(stream.AsStream(), ImageFormat.Jpeg);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, softwareBitmap.BitmapPixelFormat, BitmapAlphaMode.Premultiplied);

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);

            return source;
        }
    }
}
