using CoffeeNap.Services;

namespace CoffeeNap.Models;

public enum AddConsumptionStep
{
    DrinkType,
    CoffeeLocation,
    BrewingMethod,
    CoffeeAmount,
    CoffeeDrinkType,
    CoffeeVolume,
    CoffeeBeanType,
    TeaSort,
    TeaAmount,
    EnergyDrinkVolume,
    Result
}

public enum CoffeeLocation { Home, Outside }

public enum CoffeeBeanType { Arabica, Robusta }

public enum CoffeeBrewingMethod
{
    EspressoMachine,
    ColdBrew,
    MokaPot,
    FrenchPress,
    Turkish,
    PourOver,
    Kettle
}

public enum CoffeeDrinkType
{
    Espresso,
    Macchiato,
    Americano,
    Cappuccino,
    Latte,
    FlatWhite,
    Mocha,
    Affogato
}

public enum ServingSize { Small, Medium, Large }

public enum TeaType { Black, Green }

public sealed class AddConsumptionQuizState
{
    public CaffeineConsumptionType? DrinkType { get; set; }
    public CoffeeLocation? CoffeeLocation { get; set; }
    public CoffeeDrinkType? CoffeeDrinkType { get; set; }
    public CoffeeBrewingMethod? BrewingMethod { get; set; }
    public ServingSize? ServingSize { get; set; }
    public int? VolumeMl { get; set; }
    public string? VolumeDisplay { get; set; }
    public double? CoffeeAmountGrams { get; set; }
    public int? CoffeeSpoonCount { get; set; }
    public string? CoffeeAmountDisplay { get; set; }
    public CoffeeBeanType? BeanType { get; set; }
    public TeaType? TeaType { get; set; }
    public double? TeaAmountGrams { get; set; }
    public int? TeaSpoonCount { get; set; }
    public string? TeaAmountDisplay { get; set; }
    public int? EnergyDrinkVolumeMl { get; set; }

    public void Reset()
    {
        DrinkType = null;
        CoffeeLocation = null;
        CoffeeDrinkType = null;
        BrewingMethod = null;
        ServingSize = null;
        VolumeMl = null;
        VolumeDisplay = null;
        CoffeeAmountGrams = null;
        CoffeeSpoonCount = null;
        CoffeeAmountDisplay = null;
        BeanType = null;
        TeaType = null;
        TeaAmountGrams = null;
        TeaSpoonCount = null;
        TeaAmountDisplay = null;
        EnergyDrinkVolumeMl = null;
    }

    public void ClearAfterDrinkType()
    {
        CoffeeLocation = null;
        ClearCoffeeBranches();
        ClearTeaBranch();
        ClearEnergyDrinkBranch();
    }

    public void ClearCoffeeBranches()
    {
        CoffeeDrinkType = null;
        ServingSize = null;
        VolumeMl = null;
        VolumeDisplay = null;
        BrewingMethod = null;
        CoffeeAmountGrams = null;
        CoffeeSpoonCount = null;
        CoffeeAmountDisplay = null;
        BeanType = null;
    }

    public void ClearTeaBranch()
    {
        TeaType = null;
        TeaAmountGrams = null;
        TeaSpoonCount = null;
        TeaAmountDisplay = null;
    }

    public void ClearEnergyDrinkBranch() => EnergyDrinkVolumeMl = null;
}

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

public static class EnergyDrinkQuizCatalog
{
    public const int SmallVolumeMl = 250;
    public const int MediumVolumeMl = 330;
    public const int LargeVolumeMl = 500;
    public const double CaffeineMgPer100Ml = 32;
}

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

public static class CoffeeQuizCatalog
{
    public const int One = 1;
    public const int Two = 2;
    public const int Three = 3;
    public const double GramsPerSpoon = 8;

    public static string GetServingSizeDisplay(ServingSize servingSize) => servingSize switch
    {
        ServingSize.Small => LocalizationService.Current["Small"],
        ServingSize.Medium => LocalizationService.Current["Medium"],
        ServingSize.Large => LocalizationService.Current["Large"],
        _ => throw new ArgumentOutOfRangeException(nameof(servingSize))
    };

    public static double GetSpoonGrams(int spoonCount) => spoonCount * GramsPerSpoon;

    public static string GetSpoonDisplay(int spoonCount) => spoonCount switch
    {
        1 => LocalizationService.Current["OneSpoon"],
        2 => LocalizationService.Current["TwoSpoons"],
        3 => LocalizationService.Current["ThreeSpoons"],
        _ => $"{spoonCount} {LocalizationService.Current["SpoonMany"]}"
    };

    public static string GetBeanDisplay(CoffeeBeanType type) => type switch
    {
        CoffeeBeanType.Arabica => LocalizationService.Current["Arabica"],
        CoffeeBeanType.Robusta => LocalizationService.Current["Robusta"],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string GetLocationDisplay(CoffeeLocation location) => location switch
    {
        CoffeeLocation.Home => LocalizationService.Current["AtHome"],
        CoffeeLocation.Outside => LocalizationService.Current["AtCafe"],
        _ => throw new ArgumentOutOfRangeException(nameof(location))
    };

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
