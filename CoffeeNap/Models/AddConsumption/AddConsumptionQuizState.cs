namespace CoffeeNap.Models;

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

    // Resets all quiz answers.
    public void Reset()
    {
        DrinkType = null;
        ClearAfterDrinkType();
    }

    // Clears answers that depend on the selected drink type.
    public void ClearAfterDrinkType()
    {
        CoffeeLocation = null;
        ClearCoffeeBranches();
        ClearTeaBranch();
        ClearEnergyDrinkBranch();
    }

    // Clears all coffee-specific quiz answers.
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

    // Clears all tea-specific quiz answers.
    public void ClearTeaBranch()
    {
        TeaType = null;
        TeaAmountGrams = null;
        TeaSpoonCount = null;
        TeaAmountDisplay = null;
    }

    // Clears all energy drink quiz answers.
    public void ClearEnergyDrinkBranch() => EnergyDrinkVolumeMl = null;
}
