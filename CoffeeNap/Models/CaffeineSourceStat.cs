namespace CoffeeNap.Models;

public sealed class CaffeineSourceStat
{
    public CaffeineSourceStat(string name, double amount, double totalAmount, Color color)
    {
        Name = name;
        Amount = Math.Max(0, amount);
        TotalAmount = Math.Max(0, totalAmount);
        Color = color;
    }

    public string Name { get; }

    public double Amount { get; }

    public double TotalAmount { get; }

    public Color Color { get; }

    public double Percentage => TotalAmount <= 0 ? 0 : Amount / TotalAmount;

    public string PercentageDisplay => $"{Percentage:P0}";

    public GridLength SegmentWidth => new(Percentage, GridUnitType.Star);
}
