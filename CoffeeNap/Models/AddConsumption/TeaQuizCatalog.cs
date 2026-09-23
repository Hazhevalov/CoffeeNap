using CoffeeNap.Services;

namespace CoffeeNap.Models;

public static class TeaQuizCatalog
{
    // A loose-leaf teaspoon is approximately 2 g; leaf size varies.
    public const double GramsPerSpoon = 2;

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

}
