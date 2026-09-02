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
    public string? CoffeeAmountDisplay { get; set; }
    public CoffeeBeanType? BeanType { get; set; }

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
        CoffeeAmountDisplay = null;
        BeanType = null;
    }

    public void ClearAfterDrinkType()
    {
        CoffeeLocation = null;
        ClearCoffeeBranches();
    }

    public void ClearCoffeeBranches()
    {
        CoffeeDrinkType = null;
        ServingSize = null;
        VolumeMl = null;
        VolumeDisplay = null;
        BrewingMethod = null;
        CoffeeAmountGrams = null;
        CoffeeAmountDisplay = null;
        BeanType = null;
    }
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
        ServingSize.Small => "Маленький",
        ServingSize.Medium => "Средний",
        ServingSize.Large => "Большой",
        _ => throw new ArgumentOutOfRangeException(nameof(servingSize))
    };

    public static double GetSpoonGrams(int spoonCount) => spoonCount * GramsPerSpoon;

    public static string GetSpoonDisplay(int spoonCount) => spoonCount switch
    {
        1 => "Одна ложка",
        2 => "Две ложки",
        3 => "Три ложки",
        _ => $"{spoonCount} ложек"
    };

    public static string GetBeanDisplay(CoffeeBeanType type) => type switch
    {
        CoffeeBeanType.Arabica => "Арабика",
        CoffeeBeanType.Robusta => "Робуста",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string GetLocationDisplay(CoffeeLocation location) => location switch
    {
        CoffeeLocation.Home => "Дома",
        CoffeeLocation.Outside => "В кафе",
        _ => throw new ArgumentOutOfRangeException(nameof(location))
    };

    public static string GetBrewingMethodDisplay(CoffeeBrewingMethod method) => method switch
    {
        CoffeeBrewingMethod.EspressoMachine => "Эспрессо-машина",
        CoffeeBrewingMethod.ColdBrew => "Cold brew",
        CoffeeBrewingMethod.MokaPot => "Гейзер",
        CoffeeBrewingMethod.FrenchPress => "Френч-пресс",
        CoffeeBrewingMethod.Turkish => "Турка",
        CoffeeBrewingMethod.PourOver => "Воронка",
        CoffeeBrewingMethod.Kettle => "Чайник",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    public static string GetDrinkDisplay(CoffeeDrinkType type) => type switch
    {
        CoffeeDrinkType.Espresso => "Эспрессо",
        CoffeeDrinkType.Macchiato => "Макиато",
        CoffeeDrinkType.Americano => "Американо",
        CoffeeDrinkType.Cappuccino => "Капучино",
        CoffeeDrinkType.Latte => "Латте",
        CoffeeDrinkType.FlatWhite => "Флэт-Уайт",
        CoffeeDrinkType.Mocha => "Мокко",
        CoffeeDrinkType.Affogato => "Аффогато",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
