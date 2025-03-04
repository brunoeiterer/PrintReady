using System.Collections.Generic;

namespace PrintReady.Models;
public class LayoutManager
{
    public readonly List<LayoutItem> Items = [];
    public List<List<LayoutItem>> ComputeLayout(double targetRowHeight, double containerWidth)
    {
        List<List<LayoutItem>> rows = [];
        List<LayoutItem> currentRow = [];
        var currentRowWidth = 0d;

        foreach (var item in Items)
        {
            double scaledWidth = item.AspectRatio * targetRowHeight;
            currentRowWidth += scaledWidth;
            currentRow.Add(item);

            if (currentRowWidth >= containerWidth * 0.9)
            {
                rows.Add(AdjustRow(currentRow, currentRowWidth, targetRowHeight, containerWidth));
                currentRow = [];
                currentRowWidth = 0;
            }
        }

        if (currentRow.Count > 0)
        {
            rows.Add(AdjustLastRow(currentRow, currentRowWidth, targetRowHeight, containerWidth));
        }

        return rows;
    }

    private static List<LayoutItem> AdjustRow(List<LayoutItem> row, double rowWidth, double targetRowHeight, double containerWidth)
    {
        double scaleFactor = containerWidth / rowWidth;
        foreach (var item in row)
        {
            item.ScaledWidth = item.AspectRatio * targetRowHeight * scaleFactor;
            item.ScaledHeight = targetRowHeight * scaleFactor;
        }
        return row;
    }

    private static List<LayoutItem> AdjustLastRow(List<LayoutItem> row, double rowWidth, double targetRowHeight, double containerWidth)
    {
        double minRowWidth = containerWidth * 0.6;
        if (rowWidth < minRowWidth)
        {
            foreach (var item in row)
            {
                item.ScaledWidth = item.AspectRatio * targetRowHeight;
                item.ScaledHeight = targetRowHeight;
            }
        }
        else
        {
            row = AdjustRow(row, rowWidth, targetRowHeight, containerWidth);
        }
        return row;
    }
}
