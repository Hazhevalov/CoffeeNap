using CoffeeNap.Services;

namespace CoffeeNap.Helpers;

/// <summary>
/// Formats elapsed time with localized labels and numeric word forms.
/// Independent of the UI so formatting can be tested directly.
/// </summary>
public static class RelativeTimeFormatter
{
    /// <summary>
    /// Returns a short consumption time label relative to <paramref name="now"/>.
    /// Accepts the current time explicitly for deterministic tests.
    /// </summary>
    public static string Format(DateTimeOffset consumedAt, DateTimeOffset now)
    {
        // Future dates have negative elapsed time and also display as just now.
        var elapsed = now - consumedAt;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return LocalizationService.Current["JustNow"];
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            var minutes = Math.Max(1, (int)elapsed.TotalMinutes);
            return FormatElapsed(minutes, "MinuteOne", "MinuteFew", "MinuteMany");
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            var hours = Math.Max(1, (int)elapsed.TotalHours);
            return FormatElapsed(hours, "HourOne", "HourFew", "HourMany");
        }

        var days = Math.Max(1, (int)elapsed.TotalDays);
        return FormatElapsed(days, "DayOne", "DayFew", "DayMany");
    }

    // Formats an elapsed value with its localized time unit.
    private static string FormatElapsed(
        int value,
        string singularKey,
        string paucalKey,
        string pluralKey)
    {
        var localization = LocalizationService.Current;
        var unit = GetWordForm(
            value,
            localization[singularKey],
            localization[paucalKey],
            localization[pluralKey]);
        return localization.Format("AgoFormat", value, unit);
    }

    // Selects the word form that matches the numeric value.
    private static string GetWordForm(int value, string singular, string paucal, string plural)
    {
        // Values ending in 11 to 14 always use the plural form.
        var lastTwoDigits = value % 100;
        if (lastTwoDigits is >= 11 and <= 14)
        {
            return plural;
        }

        // Otherwise, the final digit determines the word form.
        return (value % 10) switch
        {
            1 => singular,
            2 or 3 or 4 => paucal,
            _ => plural
        };
    }
}
