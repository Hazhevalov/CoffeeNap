using CoffeeNap.Models;

namespace CoffeeNap.Services;

// Daily caffeine total calculation.
public static class CaffeineStatisticsCalculator
{
    // Totals caffeine consumed during the specified day.
    public static double CalculateDailyCaffeine(
        IEnumerable<CaffeineConsumption> consumptions,
        DateTimeOffset now)
    {
        var localNow = now.ToLocalTime();
        var startOfToday = new DateTimeOffset(
            localNow.Date,
            TimeZoneInfo.Local.GetUtcOffset(localNow.Date));

        return consumptions
            .Where(consumption =>
                consumption.ConsumedAt.ToLocalTime() >= startOfToday &&
                consumption.ConsumedAt <= now)
            .Sum(consumption => (double)Math.Max(0, consumption.CaffeineMg));
    }

}
