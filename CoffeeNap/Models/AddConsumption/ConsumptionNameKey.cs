namespace CoffeeNap.Models;

/// <summary>Resource keys are persisted instead of translated display text.</summary>
public static class ConsumptionNameKey
{
    // Resolves the localization key for a recipe's drink name.
    public static string FromRecipe(LastConsumptionRecipe recipe) => recipe.DrinkType switch
    {
        CaffeineConsumptionType.Coffee when recipe.CoffeeLocation == CoffeeLocation.Home => "HomeCoffee",
        CaffeineConsumptionType.Coffee when recipe.CoffeeDrinkType is { } drink => drink.ToString(),
        CaffeineConsumptionType.Tea when recipe.TeaType == TeaType.Black => "BlackTeaDrink",
        CaffeineConsumptionType.Tea when recipe.TeaType == TeaType.Green => "GreenTeaDrink",
        CaffeineConsumptionType.EnergyDrink => "EnergyDrink",
        _ => string.Empty
    };

    public static readonly string[] KnownKeys =
    ["HomeCoffee", "Espresso", "Macchiato", "Americano", "Cappuccino", "Latte", "FlatWhite",
     "Mocha", "Affogato", "BlackTeaDrink", "GreenTeaDrink", "EnergyDrink"];
}
