﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PrintReady.Extensions;
using PrintReady.Models;
using Windows.Storage.Pickers;

namespace PrintReady.Controls;

public sealed partial class JustifiedGallery : GridView
{
    public readonly ObservableCollection<Border> ImageBorders = [];

    private int GallerySelectedIndex { get; set; } = -1;
    private const double ItemTargetHeight = 150;
    private readonly HashSet<string> ImagePathsAdded = [];
    private readonly LayoutManager LayoutManager = new();

    public JustifiedGallery()
    {
        SelectionChanged += OnSelectionChanged;
        ItemsSource = ImageBorders;
        ImageBorders.CollectionChanged += OnImageBordersCollectionChanged;
        Loaded += OnGalleryLoaded;
        IsItemClickEnabled = true;
        ItemClick += OnGalleryItemClicked;
    }

    public void LoadImages(IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            if (!ImagePathsAdded.Add(imagePath))
            {
                continue;
            }

            var imageSource = new BitmapImage(new Uri(imagePath));
            var imageData = System.Drawing.Image.FromFile(imagePath);
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
                Child = image,
            };

            ImageBorders.Add(border);
            LayoutManager.Items.Add(new LayoutItem(imageData.Width, imageData.Height));
        }

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        var rows = LayoutManager.ComputeLayout(ItemTargetHeight, ActualWidth - 20);

        var index = 0;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (ImageBorders[index].Child is Image image)
                {
                    image.Width = row[i].ScaledWidth;
                    image.Height = row[i].ScaledHeight;
                }
                else if (ImageBorders[index].Child is FontIcon icon)
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
        Grid.SetRow(this, 0);
        Grid.SetColumn(this, 0);

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
            ImageBorders.Add(addImagesBorder);
            LayoutManager.Items.Add(new LayoutItem(ItemTargetHeight, ItemTargetHeight));
        });
    }

    private void OnImageBordersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for (var i = 0; i < ImageBorders.Count; i++)
        {
            var container = (GridViewItem)ItemContainerGenerator.ContainerFromIndex(i);
            if (container != null && ImageBorders[i].Child is Image image)
            {
                VariableSizedWrapGrid.SetColumnSpan(container, (int)image.Width + 1);
            }
            else if (container != null && ImageBorders[i].Child is FontIcon icon)
            {
                VariableSizedWrapGrid.SetColumnSpan(container, (int)icon.Width + 1);
            }
        }
    }

    private async void OnGalleryItemClicked(object sender, RoutedEventArgs e)
    {
        IsItemClickEnabled = false;

        if (e is ItemClickEventArgs args && ReferenceEquals(args.ClickedItem, ImageBorders[0]))
        {
            await AddPictures();
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

            var source = ((SelectedItem as Border)?.Child as Image)?.Source as BitmapImage;
        }
    }

    private async Task AddPictures()
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
}