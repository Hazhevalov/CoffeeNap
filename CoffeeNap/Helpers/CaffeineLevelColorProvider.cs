using CoffeeNap.Models;

namespace CoffeeNap.Helpers;

public static class CaffeineLevelColorProvider
{
    public static Color GetColor(CaffeineLevel level) => level switch
    {
        CaffeineLevel.Low => GetResourceColor("CaffeineProgressLow", Colors.Lime),
        CaffeineLevel.Medium => GetResourceColor("CaffeineProgressMedium", Colors.Yellow),
        CaffeineLevel.High => GetResourceColor("CaffeineProgressHigh", Colors.Orange),
        CaffeineLevel.LimitExceeded => GetResourceColor("CaffeineProgressLimit", Colors.Red),
        _ => GetResourceColor("SurfaceColor", Colors.White)
    };

    private static Color GetResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
