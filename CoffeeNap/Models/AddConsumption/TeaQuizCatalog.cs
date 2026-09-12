using CoffeeNap.Services;

namespace CoffeeNap.Models;

public static class TeaQuizCatalog
{
    public const double GramsPerSpoon = 2.5;
    public const double BlackCaffeineMgPerGram = 6;
    public const double GreenCaffeineMgPerGram = 5;

    public static double GetSpoonGrams(int spoonCount) => spoonCount * GramsPerSpoon;

    public static string GetSpoonDisplay(int spoonCount) => CoffeeQuizCatalog.GetSpoonDisplay(spoonCount);

    public static string GetTypeDisplay(TeaType type) => type switch
    {
        TeaType.Black => LocalizationService.Current["BlackTea"],
        TeaType.Green => LocalizationService.Current["GreenTea"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string GetDrinkDisplay(TeaType type) => type switch
    {
        TeaType.Black => LocalizationService.Current["BlackTeaDrink"],
        TeaType.Green => LocalizationService.Current["GreenTeaDrink"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static double GetCaffeineMgPerGram(TeaType type) => type switch
    {
        TeaType.Black => BlackCaffeineMgPerGram,
        TeaType.Green => GreenCaffeineMgPerGram,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
