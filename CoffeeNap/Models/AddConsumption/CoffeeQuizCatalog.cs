using CoffeeNap.Services;

namespace CoffeeNap.Models;

public static class CoffeeQuizCatalog
{
    public const int One = 1;
    public const int Two = 2;
    public const int Three = 3;
    public const double GramsPerSpoon = 8;

    // Returns the localized serving size label.
    public static string GetServingSizeDisplay(ServingSize servingSize) => servingSize switch
    {
        ServingSize.Small => LocalizationService.Current["Small"],
        ServingSize.Medium => LocalizationService.Current["Medium"],
        ServingSize.Large => LocalizationService.Current["Large"],
        _ => throw new ArgumentOutOfRangeException(nameof(servingSize))
    };

    // Converts a spoon count to grams.
    public static double GetSpoonGrams(int spoonCount) => spoonCount * GramsPerSpoon;

    // Returns the localized spoon amount label.
    public static string GetSpoonDisplay(int spoonCount) => spoonCount switch
    {
        1 => LocalizationService.Current["OneSpoon"],
        2 => LocalizationService.Current["TwoSpoons"],
        3 => LocalizationService.Current["ThreeSpoons"],
        _ => $"{spoonCount} {LocalizationService.Current["SpoonMany"]}"
    };

    // Returns the localized coffee bean label.
    public static string GetBeanDisplay(CoffeeBeanType type) => type switch
    {
        CoffeeBeanType.Arabica => LocalizationService.Current["Arabica"],
        CoffeeBeanType.Robusta => LocalizationService.Current["Robusta"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // Returns the localized coffee location label.
    public static string GetLocationDisplay(CoffeeLocation location) => location switch
    {
        CoffeeLocation.Home => LocalizationService.Current["AtHome"],
        CoffeeLocation.Outside => LocalizationService.Current["AtCafe"],
        _ => throw new ArgumentOutOfRangeException(nameof(location))
    };

    // Returns the localized brewing method label.
    public static string GetBrewingMethodDisplay(CoffeeBrewingMethod method) => method switch
    {
        CoffeeBrewingMethod.EspressoMachine => LocalizationService.Current["EspressoMachine"],
        CoffeeBrewingMethod.ColdBrew => LocalizationService.Current["ColdBrew"],
        CoffeeBrewingMethod.MokaPot => LocalizationService.Current["MokaPot"],
        CoffeeBrewingMethod.FrenchPress => LocalizationService.Current["FrenchPress"],
        CoffeeBrewingMethod.Turkish => LocalizationService.Current["Turkish"],
        CoffeeBrewingMethod.PourOver => LocalizationService.Current["PourOver"],
        CoffeeBrewingMethod.Kettle => LocalizationService.Current["Kettle"],
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    // Returns the localized drink name.
    public static string GetDrinkDisplay(CoffeeDrinkType type) => type switch
    {
        CoffeeDrinkType.Espresso => LocalizationService.Current["Espresso"],
        CoffeeDrinkType.Macchiato => LocalizationService.Current["Macchiato"],
        CoffeeDrinkType.Americano => LocalizationService.Current["Americano"],
        CoffeeDrinkType.Cappuccino => LocalizationService.Current["Cappuccino"],
        CoffeeDrinkType.Latte => LocalizationService.Current["Latte"],
        CoffeeDrinkType.FlatWhite => LocalizationService.Current["FlatWhite"],
        CoffeeDrinkType.Mocha => LocalizationService.Current["Mocha"],
        CoffeeDrinkType.Affogato => LocalizationService.Current["Affogato"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
