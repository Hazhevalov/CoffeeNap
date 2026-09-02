using System.Collections.ObjectModel;
using CoffeeNap.Models;

namespace CoffeeNap.ViewModels;

public class CalendarPageViewModel
{
    public ObservableCollection<CalendarDay> Days { get; } = new();

    public string MonthTitle { get; }

    public CalendarPageViewModel()
    {
        var currentMonth = new DateTime(2026, 9, 1);

        MonthTitle = currentMonth.ToString("MMMM yyyy");

        GenerateCalendar(currentMonth);
    }

    private void GenerateCalendar(DateTime month)
    {
        Days.Clear();

        int daysInMonth = DateTime.DaysInMonth(
            month.Year,
            month.Month);

        // Понедельник = 0
        // Вторник = 1
        // ...
        // Воскресенье = 6
        int firstDay = ((int)month.DayOfWeek + 6) % 7;

        // Пустые ячейки перед первым днем
        for (int i = 0; i < firstDay; i++)
        {
            Days.Add(new CalendarDay
            {
                Day = 0
            });
        }

        // Дни месяца
        for (int day = 1; day <= daysInMonth; day++)
        {
            int? caffeine = GetCaffeine(day);

            Days.Add(new CalendarDay
            {
                Day = day,
                Caffeine = caffeine,
                BorderColor = GetColor(caffeine),
                BorderThickness = caffeine.HasValue ? 2 : 0
            });
        }

        // Заполняем последнюю строку
        while (Days.Count % 7 != 0)
        {
            Days.Add(new CalendarDay
            {
                Day = 0
            });
        }
    }

    private int? GetCaffeine(int day)
    {
        return day switch
        {
            1 => 120,
            2 => 150,
            3 => 280,
            4 => 300,

            _ => null
        };
    }

    private string GetColor(int? caffeine)
    {
        return caffeine switch
        {
            120 => "#B5F51A",
            150 => "#F7E514",
            280 => "#FF7A16",
            300 => "#FF0000",

            _ => "Transparent"
        };
    }
}