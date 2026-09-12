namespace CoffeeNap.Models;

/// <summary>Runtime-cached statistics for the current real week.</summary>
public sealed record CalendarWeekStatistics(
    DateTime WeekStart,
    IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Days);
