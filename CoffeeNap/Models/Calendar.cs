namespace CoffeeNap.Models;

public class CalendarDay
{
    public int Day { get; set; }

    public bool IsVisible => Day > 0;

    public string BorderColor { get; set; } = "Transparent";

    public double BorderThickness { get; set; } = 0;

    public int? Caffeine { get; set; }
}