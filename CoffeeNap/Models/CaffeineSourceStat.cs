namespace CoffeeNap.Models;

public sealed class CaffeineSourceStat
{
    public CaffeineSourceStat(string name, int count, int totalCount, Color color)
    {
        Name = name;
        Count = Math.Max(0, count);
        TotalCount = Math.Max(0, totalCount);
        Color = color;
    }

    public string Name { get; }

    public int Count { get; }

    public int TotalCount { get; }

    public Color Color { get; }

    public double Ratio => TotalCount == 0 ? 0 : (double)Count / TotalCount;

    public string PercentageDisplay => $"{Ratio:P0}";

    // В слишком узком сегменте процент не поместится, поэтому скрываем подпись.
    public bool IsPercentageVisible => Ratio >= 0.08;

    // Star-ширина превращает долю источника в пропорциональный сегмент диаграммы.
    public GridLength SegmentWidth => Ratio == 0
        ? new GridLength(0)
        : new GridLength(Ratio, GridUnitType.Star);
}
