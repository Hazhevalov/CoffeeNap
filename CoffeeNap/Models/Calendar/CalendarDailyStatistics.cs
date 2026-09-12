namespace CoffeeNap.Models;

/// <summary>Aggregated immutable statistics for one local calendar day.</summary>
public readonly record struct CalendarDailyStatistics(
    int TotalCaffeineMg,
    ConsumptionTypeDistribution Distribution);
