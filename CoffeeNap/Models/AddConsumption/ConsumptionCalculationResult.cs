namespace CoffeeNap.Models;

public sealed record ConsumptionCalculationResult(
    int CaffeineMg,
    string DisplayName,
    CaffeineConsumptionType Type,
    string ContextLabel,
    string Detail1,
    string Detail2,
    string Detail2Value,
    string Detail3,
    string Detail4,
    string Detail4Value);
