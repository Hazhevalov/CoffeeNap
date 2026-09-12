using CoffeeNap.Models;

namespace CoffeeNap.Services;

// Подсчёт ДНЕВНОЙ нормы кофеина
public static class CaffeineStatisticsCalculator
{
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
            .Sum(consumption => Math.Max(0, consumption.CaffeineMg));
    }

}
