using CoffeeNap.Services;

namespace CoffeeNap.Models;

public static class TeaQuizCatalog
{
    // A loose-leaf teaspoon is approximately 2 g; leaf size varies.
    public const double GramsPerSpoon = 2;

    // Converts a spoon count to grams.
    public static double GetSpoonGrams(int spoonCount) => spoonCount * GramsPerSpoon;

    // Returns the localized spoon amount label.
    public static string GetSpoonDisplay(int spoonCount) => CoffeeQuizCatalog.GetSpoonDisplay(spoonCount);

    // Returns the localized tea type label.
    public static string GetTypeDisplay(TeaType type) => type switch
    {
        TeaType.Black => LocalizationService.Current["BlackTea"],
        TeaType.Green => LocalizationService.Current["GreenTea"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // Returns the localized drink name.
    public static string GetDrinkDisplay(TeaType type) => type switch
    {
        TeaType.Black => LocalizationService.Current["BlackTeaDrink"],
        TeaType.Green => LocalizationService.Current["GreenTeaDrink"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

}
