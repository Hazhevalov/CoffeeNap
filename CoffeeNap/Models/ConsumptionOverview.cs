namespace CoffeeNap.Models;

// Stores the timestamp and identifier used to resume history pagination.
public readonly record struct ConsumptionCursor(DateTimeOffset ConsumedAt, int Id);

// Combines the daily caffeine total with consumption source counts.
public sealed record ConsumptionOverview(double DailyCaffeineMg, ConsumptionTypeDistribution Distribution);
