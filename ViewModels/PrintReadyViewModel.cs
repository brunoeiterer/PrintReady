using System.Collections.ObjectModel;
using System.Drawing;
using Microsoft.UI.Xaml.Controls;

namespace PrintReady.ViewModels
{
    public class PrintReadyViewModel
    {
        public readonly ObservableCollection<Border> ImageBorders = [];
        public Color SelectedColor { get; set; } = Color.White;
        public int SelectedResolution { get; set; } = 300;
        public (int side1, int side2) SelectedSize { get; set; } = (4, 6);
    }
}
