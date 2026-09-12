namespace CoffeeNap.Models;

public readonly record struct CalendarInitialStatistics(
    CalendarMonthStatistics Month,
    CalendarWeekStatistics Week);
