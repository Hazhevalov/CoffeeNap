namespace CoffeeNap.Helpers;

public static class RelativeTimeFormatter
{
    public static string Format(DateTimeOffset consumedAt, DateTimeOffset now)
    {
        var elapsed = now - consumedAt;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "только что";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            var minutes = Math.Max(1, (int)elapsed.TotalMinutes);
            return $"{minutes} {GetWordForm(minutes, "минуту", "минуты", "минут")} назад";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            var hours = Math.Max(1, (int)elapsed.TotalHours);
            return $"{hours} {GetWordForm(hours, "час", "часа", "часов")} назад";
        }

        var days = Math.Max(1, (int)elapsed.TotalDays);
        return $"{days} {GetWordForm(days, "день", "дня", "дней")} назад";
    }

    private static string GetWordForm(int value, string singular, string paucal, string plural)
    {
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
