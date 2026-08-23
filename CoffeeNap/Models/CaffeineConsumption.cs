using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.Models;

public sealed class CaffeineConsumption : ObservableObject
{
    private CaffeineConsumptionType type;

    public required string Name { get; init; }

    public required DateTimeOffset ConsumedAt { get; init; }

    public required int CaffeineMg { get; init; }

    public required string Icon { get; init; }

    public required Color IconBackground { get; init; }

    public required CaffeineConsumptionType Type
    {
        get => type;
        set => SetProperty(ref type, value);
    }

    public string CaffeineDisplay => $"{CaffeineMg} мг";

    public string RelativeTime => FormatRelativeTime(DateTimeOffset.Now - ConsumedAt);

    private static string FormatRelativeTime(TimeSpan elapsed)
    {
        // Выбираем наиболее крупную подходящую единицу времени, чтобы подпись
        // оставалась короткой и естественно читалась в списке.
        if (elapsed.TotalMinutes < 1)
        {
            return "только что";
        }

        if (elapsed.TotalHours < 1)
        {
            var minutes = Math.Max(1, (int)elapsed.TotalMinutes);
            return $"{minutes} {GetWordForm(minutes, "минуту", "минуты", "минут")} назад";
        }

        if (elapsed.TotalDays < 1)
        {
            var hours = Math.Max(1, (int)elapsed.TotalHours);
            return $"{hours} {GetWordForm(hours, "час", "часа", "часов")} назад";
        }

        var days = Math.Max(1, (int)elapsed.TotalDays);
        return $"{days} {GetWordForm(days, "день", "дня", "дней")} назад";
    }

    private static string GetWordForm(int value, string singular, string paucal, string plural)
    {
        // Числа от 11 до 14 — исключение из обычного правила,
        // основанного на последней цифре числа.
        var lastTwoDigits = value % 100;
        if (lastTwoDigits is >= 11 and <= 14)
        {
            return plural;
        }

        return (value % 10) switch
        {
            1 => singular,
            2 or 3 or 4 => paucal,
            _ => plural
        };
    }
}
