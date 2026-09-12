namespace CoffeeNap.Models;

/// <summary>Runtime-cached statistics for one calendar month.</summary>
public sealed record CalendarMonthStatistics(
    DateTime Month,
    IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Days,
    int BestComboDays,
    int MostConsumedInDayMg);
