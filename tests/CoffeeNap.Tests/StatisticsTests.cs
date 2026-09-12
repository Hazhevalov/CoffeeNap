using CoffeeNap.Models;
using CoffeeNap.Services;

namespace CoffeeNap.Tests;

public sealed class StatisticsTests
{
    [Theory]
    [InlineData(-1, CaffeineLevel.None)]
    [InlineData(0, CaffeineLevel.None)]
    [InlineData(1, CaffeineLevel.Low)]
    [InlineData(119, CaffeineLevel.Low)]
    [InlineData(120, CaffeineLevel.Medium)]
    [InlineData(209, CaffeineLevel.Medium)]
    [InlineData(210, CaffeineLevel.High)]
    [InlineData(299, CaffeineLevel.High)]
    [InlineData(300, CaffeineLevel.LimitExceeded)]
    public void DailyLevelsKeepTheirBoundaries(int total, CaffeineLevel expected) =>
        Assert.Equal(expected, CaffeineLevelResolver.ResolveDailyTotal(total));

    [Theory]
    [InlineData(0, CaffeineLevel.Low)]
    [InlineData(0.4, CaffeineLevel.Low)]
    [InlineData(0.401, CaffeineLevel.Medium)]
    [InlineData(0.7, CaffeineLevel.Medium)]
    [InlineData(0.701, CaffeineLevel.High)]
    [InlineData(0.999, CaffeineLevel.High)]
    [InlineData(1, CaffeineLevel.LimitExceeded)]
    [InlineData(2, CaffeineLevel.LimitExceeded)]
    public void ProgressKeepsItsBoundaries(double ratio, CaffeineLevel expected) =>
        Assert.Equal(expected, CaffeineLevelResolver.ResolveProgress(ratio));

    [Fact]
    public void DailyTotalIncludesLocalMidnightAndNowButNotYesterdayOrFuture()
    {
        var midnight = new DateTimeOffset(new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Local));
        var now = midnight.AddHours(12);
        CaffeineConsumption[] items =
        [
            new() { ConsumedAt = midnight.AddTicks(-1), CaffeineMg = 200 },
            new() { ConsumedAt = midnight.ToUniversalTime(), CaffeineMg = 30 },
            new() { ConsumedAt = now, CaffeineMg = 80 },
            new() { ConsumedAt = now.AddTicks(1), CaffeineMg = 300 },
            new() { ConsumedAt = now.AddHours(-1), CaffeineMg = -20 }
        ];
        Assert.Equal(110, CaffeineStatisticsCalculator.CalculateDailyCaffeine(items, now));
        Assert.Equal(0, CaffeineStatisticsCalculator.CalculateDailyCaffeine([], now));
    }

    [Fact]
    public void DistributionCountsDrinksRatherThanMilligrams()
    {
        CaffeineConsumption[] items =
        [
            new() { Type = CaffeineConsumptionType.Coffee, CaffeineMg = 200 },
            new() { Type = CaffeineConsumptionType.Coffee, CaffeineMg = 100 },
            new() { Type = CaffeineConsumptionType.Tea, CaffeineMg = 10 },
            new() { Type = CaffeineConsumptionType.EnergyDrink, CaffeineMg = 80 },
            new() { Type = (CaffeineConsumptionType)99, CaffeineMg = 1000 }
        ];
        var result = ConsumptionTypeDistributionCalculator.Calculate(items);
        Assert.Equal(new ConsumptionTypeDistribution(2, 1, 1), result);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(0.5, result.CoffeeRatio);
        Assert.Equal(0.25, result.TeaRatio);
        Assert.Equal(0.25, result.EnergyDrinkRatio);
        var empty = ConsumptionTypeDistributionCalculator.Calculate([]);
        Assert.Equal(0, empty.TotalCount);
        Assert.Equal(0, empty.CoffeeRatio + empty.TeaRatio + empty.EnergyDrinkRatio);
    }
}
