namespace CoffeeNap.Models;

/// <summary>Count-based caffeine-source distribution for one day of the current week.</summary>
public sealed class WeeklyConsumptionItem
{
    public DateTime Date { get; init; }
    public DayOfWeek DayOfWeek => Date.DayOfWeek;
    public string DayLabel { get; init; } = string.Empty;
    public bool IsToday { get; init; }
    public ConsumptionTypeDistribution Distribution { get; init; }
    public int CoffeeCount => Distribution.CoffeeCount;
    public int TeaCount => Distribution.TeaCount;
    public int EnergyDrinkCount => Distribution.EnergyDrinkCount;
    public int TotalCount => Distribution.TotalCount;
    public double CoffeeRatio => Distribution.CoffeeRatio;
    public double TeaRatio => Distribution.TeaRatio;
    public double EnergyDrinkRatio => Distribution.EnergyDrinkRatio;
    public double EmptyRatio => TotalCount == 0 ? 1 : 0;
}
