namespace CoffeeNap.Models;

// Combines month and week statistics for the initial calendar load.
public readonly record struct CalendarInitialStatistics(
    CalendarMonthStatistics Month,
    CalendarWeekStatistics Week);
