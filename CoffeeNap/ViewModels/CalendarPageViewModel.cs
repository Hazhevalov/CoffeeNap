using System.Collections.ObjectModel;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.ViewModels;

public class CalendarPageViewModel : ObservableObject
{
    private readonly LocalizationService _localization;
    private readonly DateTime _currentMonth = new(2026, 9, 1);
    private string _monthTitle = string.Empty;

    public ObservableCollection<CalendarDay> Days { get; } = new();

    public string MonthTitle
    {
        get => _monthTitle;
        private set => SetProperty(ref _monthTitle, value);
    }

    public CalendarPageViewModel(LocalizationService localization)
    {
        _localization = localization;
        UpdateLocalizedState();
        GenerateCalendar(_currentMonth);
        _localization.CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs eventArgs) => UpdateLocalizedState();

    private void UpdateLocalizedState() =>
        MonthTitle = _currentMonth.ToString("MMMM yyyy", _localization.CurrentCulture);

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
