using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PrintReady.Extensions;
using PrintReady.ViewModels;
using Windows.Storage.Pickers;

namespace PrintReady.Controls
{
    public sealed partial class PrintReadyCommandBar : UserControl
    {
        private readonly PrintReadyViewModel ViewModel;

        public event EventHandler? ExportRequested;
        public PrintReadyCommandBar()
        {
            InitializeComponent();
            ViewModel = (PrintReadyViewModel?)App.Services.GetService(typeof(PrintReadyViewModel)) ?? throw new NullReferenceException();
        }

        public void OnBorderColorSelected(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem) {
                ViewModel.SelectedColor = menuFlyoutItem.Tag switch
                {
                    "White" => Color.White,
                    "Black" => Color.Black,
                    "Gray" => Color.Gray,
                    "Beige" => Color.Beige,
                    "Brown" => Color.FromArgb(unchecked((int)0xFF8B4513)),
                    "Gold" => Color.Gold,
                    "Navy" => Color.Navy,
                    "Teal" => Color.Teal,
                    "ForestGreen" => Color.ForestGreen,
                    "Red" => Color.Red,
                    "Burgundy" => Color.FromArgb(unchecked((int)0xFF800020)),
                    "RoyalBlue" => Color.RoyalBlue,
                    _ => throw new InvalidOperationException($"Selected color is invalid")
                };

                if(BorderColorButton.Content is Border border)
                {
                    border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(
                        ViewModel.SelectedColor.A, ViewModel.SelectedColor.R, ViewModel.SelectedColor.G, ViewModel.SelectedColor.B));
                }
            }
        }

        public async void OnAddPicturesAsync(object sender, RoutedEventArgs e)
        {
            if (App.Current is not App app || app.Window is not MainWindow mainWindow)
            {
                return;
            }

            await mainWindow.OnAddPicturesAsync(sender, e);
        }

        public async void OnExportButtonClickAsync(object sender, RoutedEventArgs e)
        {
            if(App.Current is not App app)
            {
                return;
            }

            var picker = new FolderPicker();

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(app.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

            var outputFolder = await picker.PickSingleFolderAsync();

            if (outputFolder == null)
            {
                return;
            }

            var progressRing = new ProgressRing()
            {
                IsIndeterminate = false,
                Value = 0
            };
            var progressDialog = new ContentDialog()
            {
                XamlRoot = XamlRoot,
                Content = progressRing,
                Title = "Exporting Images"
            };

            var progressStep = 100d / ViewModel.ImageBorders.Count;

            _ = progressDialog.ShowAsync();

            foreach (var imageBorder in ViewModel.ImageBorders)
            {
                progressRing.Value += progressStep;
                var source = (imageBorder.Child as Microsoft.UI.Xaml.Controls.Image)?.Source as BitmapImage;
                var path = source?.UriSource.LocalPath;

                if (path == null)
                {
                    continue;
                }

                var image = System.Drawing.Image.FromFile(path);
                Bitmap printReadyImage;
                if (image.Width > image.Height)
                {
                    printReadyImage = image.ToPrintReadyImage(1800, 1200, ViewModel.SelectedColor);
                }
                else
                {
                    printReadyImage = image.ToPrintReadyImage(1200, 1800, ViewModel.SelectedColor);
                }

                var extension = Path.GetExtension(path);
                var filename = Path.GetFileNameWithoutExtension(path);
                await Task.Run(() => printReadyImage.Save(Path.Combine(outputFolder.Path, $"{filename}_PrintReady.{extension}")));
            }

            progressDialog.Hide();
        }
    }
}
