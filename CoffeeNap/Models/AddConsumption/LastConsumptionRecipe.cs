using CoffeeNap.Data;
using SQLite;

namespace CoffeeNap.Models;

/// <summary>
/// Stores the last successfully saved quiz answers in at most one row.
/// Calculated and localized values are not persisted here.
/// </summary>
[Table("LastConsumptionRecipes")]
public sealed class LastConsumptionRecipe
{
    [PrimaryKey]
    public int Id { get; set; } = DatabaseConstants.LastConsumptionRecipeId;

    public CaffeineConsumptionType DrinkType { get; set; }

    public CoffeeLocation? CoffeeLocation { get; set; }
    public CoffeeDrinkType? CoffeeDrinkType { get; set; }
    public CoffeeBrewingMethod? BrewingMethod { get; set; }
    public CoffeeBeanType? BeanType { get; set; }
    public double? CoffeeAmountGrams { get; set; }
    public int? CoffeeSpoonCount { get; set; }
    public int? VolumeMl { get; set; }
    public ServingSize? ServingSize { get; set; }

    public TeaType? TeaType { get; set; }
    public double? TeaAmountGrams { get; set; }
    public int? TeaSpoonCount { get; set; }

    public int? EnergyDrinkVolumeMl { get; set; }
}
