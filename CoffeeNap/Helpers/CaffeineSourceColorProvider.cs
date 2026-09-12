using CoffeeNap.Models;

namespace CoffeeNap.Helpers;

public static class CaffeineSourceColorProvider
{
    public static Color GetColor(CaffeineConsumptionType type) => type switch
    {
        CaffeineConsumptionType.Coffee => GetResourceColor("CaffeeIcoFill", Colors.Black),
        CaffeineConsumptionType.Tea => GetResourceColor("TeaIcoFill", Color.FromArgb("#939393")),
        CaffeineConsumptionType.EnergyDrink => GetResourceColor("EnergyDrinkIcoFill", Color.FromArgb("#535353")),
        _ => Colors.Transparent
    };

    private static Color GetResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
