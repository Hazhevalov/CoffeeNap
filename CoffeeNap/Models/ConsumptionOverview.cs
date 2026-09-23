namespace CoffeeNap.Models;

public readonly record struct ConsumptionCursor(DateTimeOffset ConsumedAt, int Id);

public sealed record ConsumptionOverview(double DailyCaffeineMg, ConsumptionTypeDistribution Distribution);
