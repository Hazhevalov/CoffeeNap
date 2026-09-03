using CoffeeNap.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.Data;

internal static class DebugConsumptionSeeder
{
    private const string SeedNamePrefix = "DEBUG Calendar";

    public static async Task SeedCurrentWeekAsync(
        AppDatabase database,
        ILogger logger)
    {
        // The batch is inserted only once per database, so later DEBUG launches
        // and later weeks do not accumulate additional test history.
        if (await database.HasConsumptionNamePrefixAsync(SeedNamePrefix))
        {
            return;
        }

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        // Mon..Sun: 4, 3, 3, 4, 3, 2, 1 records respectively.
        // Totals for the week: 10 Coffee, 5 Tea, 5 EnergyDrink.
        CaffeineConsumptionType[][] distribution =
        [
            [CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Tea, CaffeineConsumptionType.EnergyDrink],
            [CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Tea, CaffeineConsumptionType.EnergyDrink],
            [CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Coffee, CaffeineConsumptionType.EnergyDrink],
            [CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Tea, CaffeineConsumptionType.EnergyDrink],
            [CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Tea, CaffeineConsumptionType.EnergyDrink],
            [CaffeineConsumptionType.Coffee, CaffeineConsumptionType.Tea],
            [CaffeineConsumptionType.Coffee]
        ];

        var consumptions = new List<CaffeineConsumption>(20);
        var sequence = 1;
        for (var dayIndex = 0; dayIndex < distribution.Length; dayIndex++)
        {
            var date = weekStart.AddDays(dayIndex);
            for (var itemIndex = 0; itemIndex < distribution[dayIndex].Length; itemIndex++)
            {
                var type = distribution[dayIndex][itemIndex];
                consumptions.Add(new CaffeineConsumption
                {
                    Name = $"{SeedNamePrefix} {type} {sequence:00}",
                    Type = type,
                    CaffeineMg = type switch
                    {
                        CaffeineConsumptionType.Coffee => 80,
                        CaffeineConsumptionType.Tea => 40,
                        CaffeineConsumptionType.EnergyDrink => 120,
                        _ => 0
                    },
                    ConsumedAt = ToUtcBoundary(
                        date.AddHours(8).AddMinutes(itemIndex * 45))
                });
                sequence++;
            }
        }

        await database.InsertConsumptionsAsync(consumptions);
        logger.LogInformation(
            "Seeded {Count} DEBUG calendar consumptions for week starting {WeekStart:yyyy-MM-dd}.",
            consumptions.Count,
            weekStart);
    }

    private static DateTimeOffset ToUtcBoundary(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(
                unspecified,
                TimeZoneInfo.Local.GetUtcOffset(unspecified))
            .ToUniversalTime();
    }
}
