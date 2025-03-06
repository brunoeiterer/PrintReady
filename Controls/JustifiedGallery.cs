using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PrintReady.Extensions;
using PrintReady.Models;
using PrintReady.ViewModels;
using Windows.Storage.Pickers;

namespace PrintReady.Controls;

public sealed partial class JustifiedGallery : GridView
{
    private readonly PrintReadyViewModel ViewModel;

    private int GallerySelectedIndex { get; set; } = -1;
    private const double ItemTargetHeight = 150;
    private readonly HashSet<string> ImagePathsAdded = [];
    private readonly LayoutManager LayoutManager = new();

    public JustifiedGallery()
    {
        ViewModel = (PrintReadyViewModel?)App.Services.GetService(typeof(PrintReadyViewModel)) ?? throw new NullReferenceException();
        SelectionChanged += OnSelectionChanged;
        ItemsSource = ViewModel.ImageBorders;
        ViewModel.ImageBorders.CollectionChanged += OnImageBordersCollectionChanged;
        Loaded += OnGalleryLoaded;
        IsItemClickEnabled = true;
        ItemClick += OnGalleryItemClicked;
    }

    public async Task LoadImages(IEnumerable<string> imagePaths)
    {
        var progressRing = new ProgressRing()
        {
            IsIndeterminate = false,
            Value = 0
        };
        var progressDialog = new ContentDialog()
        {
            XamlRoot = XamlRoot,
            Content = progressRing
        };

        var progressStep = 100d / imagePaths.Count();

        progressDialog.Title = "Loading Images";
        _ = progressDialog.ShowAsync();

        foreach (var imagePath in imagePaths)
        {
            progressRing.Value += progressStep;
            if (!ImagePathsAdded.Add(imagePath))
            {
                continue;
            }

            var imageSource = new BitmapImage(new Uri(imagePath));
            var imageData = await Task.Run(() => System.Drawing.Image.FromFile(imagePath));

            imageData.FixOrientation();

            var scaleFactor = ItemTargetHeight / imageData.Height;

            var image = new Image
            {
                Source = imageSource,
                Width = imageData.Width * scaleFactor,
                Height = ItemTargetHeight,
                Stretch = Stretch.Fill
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = image
            };

            ViewModel.ImageBorders.Add(border);
            LayoutManager.Items.Add(new LayoutItem(imageData.Width, imageData.Height));

            RefreshLayout();
        }

        progressDialog.Hide();
    }

    private void RefreshLayout()
    {
        var rows = LayoutManager.ComputeLayout(ItemTargetHeight, ActualWidth - 20);

        var index = 0;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (ViewModel.ImageBorders[index].Child is Image image)
                {
                    image.Width = row[i].ScaledWidth;
                    image.Height = row[i].ScaledHeight;
                }
                else if (ViewModel.ImageBorders[index].Child is FontIcon icon)
                {
                    icon.Width = row[i].ScaledWidth;
                    icon.Height = row[i].ScaledHeight;
                }

                index++;
            }
        }

        OnImageBordersCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnGalleryLoaded(object sender, RoutedEventArgs e)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        var itemsPanelXaml = @$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                                   <VariableSizedWrapGrid Orientation=""Horizontal""
                                                          HorizontalAlignment=""Left""
                                                          HorizontalChildrenAlignment=""Left""
                                                          ItemWidth=""1""
                                                          ItemHeight=""{ItemTargetHeight}""/>
                               </ItemsPanelTemplate>";
        var itemsPanel = (ItemsPanelTemplate)XamlReader.Load(itemsPanelXaml);
        ItemsPanel = itemsPanel;

        var addImagesBorder = new Border()
        {
            Child = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                Glyph = "\ue710",
                FontSize = 64,
                Width = ItemTargetHeight,
                Height = ItemTargetHeight,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            }
        };

        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.ImageBorders.Add(addImagesBorder);
            LayoutManager.Items.Add(new LayoutItem(ItemTargetHeight, ItemTargetHeight));
        });
    }

    private void OnImageBordersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for (var i = 0; i < ViewModel.ImageBorders.Count; i++)
        {
            var container = (GridViewItem)ItemContainerGenerator.ContainerFromIndex(i);
            if (container != null && ViewModel.ImageBorders[i].Child is Image image)
            {
                VariableSizedWrapGrid.SetColumnSpan(container, (int)image.Width + 1);
            }
            else if (container != null && ViewModel.ImageBorders[i].Child is FontIcon icon)
            {
                VariableSizedWrapGrid.SetColumnSpan(container, (int)icon.Width + 1);
            }
        }
    }

    private async void OnGalleryItemClicked(object sender, RoutedEventArgs e)
    {
        IsItemClickEnabled = false;

        if(e is not ItemClickEventArgs args)
        {
            IsItemClickEnabled = true;
            return;
        }

        if (ReferenceEquals(args.ClickedItem, ViewModel.ImageBorders[0]))
        {
            await AddPicturesAsync();
        }
        else if(args.ClickedItem is Border border && border.Child is Image image && image.Source is BitmapImage source)
        {

            var preview = await GetPreview(source.UriSource.LocalPath);
            var flyout = new Flyout()
            {
                Content = new Border()
                {
                    Child = new Image()
                    {
                        Source = preview.Source,
                        Width = preview.Width,
                        Height = preview.Height,
                    }
                },
            };

            var style = new Style(typeof(FlyoutPresenter));
            style.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
            style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
            style.Setters.Add(new Setter(MaxWidthProperty, preview.Width));
            style.Setters.Add(new Setter(MaxHeightProperty, preview.Height));
            style.Setters.Add(new Setter(WidthProperty, preview.Width));
            style.Setters.Add(new Setter(HeightProperty, preview.Height));
            flyout.SetValue(Flyout.FlyoutPresenterStyleProperty, style);

            flyout.ShowAt(border);
        }

        IsItemClickEnabled = true;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedIndex == 0)
        {
            SelectedIndex = GallerySelectedIndex;
        }
        else
        {
            GallerySelectedIndex = SelectedIndex;
        }
    }

    public async Task AddPicturesAsync()
    {
        if (App.Current is not App app)
        {
            return;
        }

        var picker = new FileOpenPicker();

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(app.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");

        var files = await picker.PickMultipleFilesAsync();
        LoadImages(files.Select(f => f.Path));
    }

    private async Task<Image> GetPreview(string imagePath)
    {
        var image = System.Drawing.Image.FromFile(imagePath);
        image.FixOrientation();

        int resizedWidth;
        int resizedHeight;
        if (image.Height > image.Width)
        {
            resizedHeight = 500;
            resizedWidth = (int)(500 / 1.5);
        }
        else
        {
            resizedHeight = (int)(500 / 1.5);
            resizedWidth = 500;
        }

        var resizedImageSource = await image.ToPrintReadyImage(resizedWidth, resizedHeight, ViewModel.SelectedColor).ToImageSourceAsync();

        var resizedImage = new Image
        {
            Source = resizedImageSource,
            Width = resizedWidth,
            Height = resizedHeight,
            Stretch = Stretch.None
        };

        return resizedImage;
    }
}