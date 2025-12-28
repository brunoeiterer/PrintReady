using System;
using System.Drawing;
using System.Linq;

namespace PrintReady.Extensions
{
    public static class ImageExtensions
    {
        private const int PropertyTagOrientation = 0x112;

        public static void FixOrientation(this Image image)
        {
            if (!image.PropertyIdList.Contains(PropertyTagOrientation))
            {
                return;
            }

            var propertyTagOrientationValue = image.GetPropertyItem(PropertyTagOrientation)?.Value;

            if (propertyTagOrientationValue == null)
            {
                return;
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
        }

        public static Bitmap ToPrintReadyImage(this Image originalImage, int width, int height, Color borderColor, int resolution, DateTimeOffset? dateTaken)
        {
            var scaleX = (double)width / originalImage.Width;
            var scaleY = (double)height / originalImage.Height;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(originalImage.Width * scale);
            var newHeight = (int)(originalImage.Height * scale);

            var offsetX = (width - newWidth) / 2;
            var offsetY = (height - newHeight) / 2;

            var borderedImage = new Bitmap(width, height, originalImage.PixelFormat);
            borderedImage.SetResolution(resolution, resolution);

            using var graphics = Graphics.FromImage(borderedImage);
            graphics.Clear(borderColor);
            graphics.DrawImage(originalImage, new Rectangle(offsetX, offsetY, newWidth, newHeight));


            var shouldAddDate = false;
            if (App.Current is App app && app.Window is MainWindow mainWindow)
            {
                shouldAddDate = mainWindow.ShouldAddDate;
            }

            if (shouldAddDate && dateTaken is not null)
            {
                var dateString = dateTaken.ToString();
                using var font = SystemFonts.DefaultFont;
                using var brush = new SolidBrush(Color.Black);
                var size = graphics.MeasureString(dateString, font);
                graphics.DrawString(dateString, font, brush, width - size.Width, height - size.Height);
            }

            return borderedImage;
        }
    }
}
