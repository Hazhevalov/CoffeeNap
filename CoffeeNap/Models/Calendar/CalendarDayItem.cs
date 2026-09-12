namespace CoffeeNap.Models;

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
