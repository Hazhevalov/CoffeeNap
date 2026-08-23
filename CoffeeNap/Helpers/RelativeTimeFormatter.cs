namespace CoffeeNap.Helpers;

/// <summary>
/// Форматирует прошедшее время по-русски и выбирает правильную форму слова
/// («1 минута», «2 минуты», «5 минут»). Не зависит от UI и легко тестируется.
/// </summary>
public static class RelativeTimeFormatter
{
    /// <summary>
    /// Возвращает короткую подпись о времени употребления относительно <paramref name="now"/>.
    /// Текущий момент передаётся параметром, чтобы результат можно было детерминированно тестировать.
    /// </summary>
    public static string Format(DateTimeOffset consumedAt, DateTimeOffset now)
    {
        // Для будущей даты elapsed отрицателен и также отображается как «только что».
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
        // 11–14 всегда используют множественную форму, несмотря на последнюю цифру.
        var lastTwoDigits = value % 100;
        if (lastTwoDigits is >= 11 and <= 14)
        {
            return plural;
        }

        // В остальных случаях достаточно проверить последнюю цифру.
        return (value % 10) switch
        {
            1 => singular,
            2 or 3 or 4 => paucal,
            _ => plural
        };
    }
}
