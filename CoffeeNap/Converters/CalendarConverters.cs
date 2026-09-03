using System.Globalization;
using CoffeeNap.Models;

namespace CoffeeNap.Converters;

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

public sealed class CaffeineLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CaffeineLevel level
            ? CaffeineLevelColorProvider.GetColor(level)
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class RatioToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = value is double number ? Math.Max(0, number) : 0;
        return new GridLength(ratio, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
