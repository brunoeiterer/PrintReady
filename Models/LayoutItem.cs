namespace PrintReady.Models;

public class LayoutItem(double width, double height)
{
    public double AspectRatio { get; set; } = width / height;
    public double ScaledWidth { get; set; } = width;
    public double ScaledHeight { get; set; } = height;
}