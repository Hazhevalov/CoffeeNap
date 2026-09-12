using CoffeeNap.Models;
using CoffeeNap.Services;
using CoffeeNap.Helpers;

namespace CoffeeNap.ViewModels;

// Объект "Ваши источники кофеина" на главном экране
public sealed class CaffeineSourceStatViewModel
{
    public CaffeineSourceStatViewModel(
        CaffeineConsumptionType type,
        int count,
        int totalCount)
    {
        Type = type;
        Count = Math.Max(0, count);
        TotalCount = Math.Max(0, totalCount);
    }

    public CaffeineConsumptionType Type { get; }

    public int Count { get; }

    public int TotalCount { get; }

    public string Name => Type switch
    {
        CaffeineConsumptionType.Coffee => LocalizationService.Current["Coffee"],
        CaffeineConsumptionType.Tea => LocalizationService.Current["Tea"],
        CaffeineConsumptionType.EnergyDrink => LocalizationService.Current["EnergyDrinks"],
        _ => string.Empty
    };

    public Color Color => CaffeineSourceColorProvider.GetColor(Type);

    public double Ratio => TotalCount == 0 ? 0 : (double)Count / TotalCount;

    public string PercentageDisplay => $"{Ratio:P0}";

    public bool IsPercentageVisible => Ratio >= 0.08;

    public GridLength SegmentWidth => Ratio == 0
        ? new GridLength(0)
        : new GridLength(Ratio, GridUnitType.Star);
}
