namespace CoffeeNap.Models;

/// <summary>Business level used by both daily progress and calendar presentation.</summary>
public enum CaffeineLevel
{
    None,
    Low,
    Medium,
    High,
    LimitExceeded
}

/// <summary>One visual slot in a Monday-first calendar grid.</summary>
public sealed class CalendarDayItem
{
    public DateTime Date { get; init; }
    public int DayNumber { get; init; }
    public bool IsCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public int TotalCaffeineMg { get; init; }
    public CaffeineLevel Level { get; init; }
}

/// <summary>Aggregated immutable statistics for one local calendar day.</summary>
public readonly record struct CalendarDailyStatistics(
    int TotalCaffeineMg,
    ConsumptionTypeDistribution Distribution);

/// <summary>Runtime-cached statistics for one calendar month.</summary>
public sealed record CalendarMonthStatistics(
    DateTime Month,
    IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Days,
    int BestComboDays,
    int MostConsumedInDayMg);

/// <summary>Runtime-cached statistics for the current real week.</summary>
public sealed record CalendarWeekStatistics(
    DateTime WeekStart,
    IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Days);

public readonly record struct CalendarInitialStatistics(
    CalendarMonthStatistics Month,
    CalendarWeekStatistics Week);

public readonly record struct ConsumptionTypeDistribution(
    int CoffeeCount,
    int TeaCount,
    int EnergyDrinkCount)
{
    public int TotalCount => CoffeeCount + TeaCount + EnergyDrinkCount;
    public double CoffeeRatio => GetRatio(CoffeeCount);
    public double TeaRatio => GetRatio(TeaCount);
    public double EnergyDrinkRatio => GetRatio(EnergyDrinkCount);

    public int GetCount(CaffeineConsumptionType type) => type switch
    {
        CaffeineConsumptionType.Coffee => CoffeeCount,
        CaffeineConsumptionType.Tea => TeaCount,
        CaffeineConsumptionType.EnergyDrink => EnergyDrinkCount,
        _ => 0
    };

    private double GetRatio(int count) => TotalCount == 0 ? 0 : (double)count / TotalCount;
}

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
