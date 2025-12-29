using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;

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

        public static int GetFixOrientationRotation(this Image image)
        {
            if (!image.PropertyIdList.Contains(PropertyTagOrientation))
            {
                return 0;
            }

            var propertyTagOrientationValue = image.GetPropertyItem(PropertyTagOrientation)?.Value;

            if (propertyTagOrientationValue == null)
            {
                return 0;
            }

            int orientation = BitConverter.ToUInt16(propertyTagOrientationValue, 0);
            if (orientation == 3)
            {
                return 180;
            }
            else if (orientation == 6)
            {
                return 90;
            }
            else if (orientation == 8)
            {
                return 270;
            }

            return 0;
        }

        public static Bitmap ToPrintReadyImage(this Image originalImage, int width, int height, Color borderColor, int resolution, DateTimeOffset? dateTaken, int? fontSize = null, int? rotation = null)
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
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.Clear(borderColor);
            graphics.DrawImage(originalImage, new Rectangle(offsetX, offsetY, newWidth, newHeight));


            var shouldAddDate = false;
            if (App.Current is App app && app.Window is MainWindow mainWindow)
            {
                shouldAddDate = mainWindow.ShouldAddDate;
            }

            if (shouldAddDate && dateTaken is not null)
            {
                var dateString = dateTaken.Value.ToString();
                using var systemFont = SystemFonts.DefaultFont;
                var font = new Font(systemFont.FontFamily, fontSize ?? 16);
                var size = graphics.MeasureString(dateString, font);

                var path = new GraphicsPath();
                path.AddString(dateString, systemFont.FontFamily, (int)FontStyle.Regular, graphics.DpiY * (fontSize ?? 16) / 72, new PointF(width - size.Width, height - size.Height), StringFormat.GenericDefault);

                graphics.DrawPath(Pens.White, path);
                graphics.FillPath(Brushes.Black, path);
            }

            if(rotation is not null)
            {
                var rotateFlipType = rotation switch
                {
                    90 => RotateFlipType.Rotate90FlipNone,
                    180 => RotateFlipType.Rotate180FlipNone,
                    270 => RotateFlipType.Rotate270FlipNone,
                    _ => RotateFlipType.RotateNoneFlipNone
                };
                borderedImage.RotateFlip(rotateFlipType);
            }

            return borderedImage;
        }
    }
}
