using System.Collections.Frozen;
using CoffeeNap.Models;

namespace CoffeeNap.Services;

// Stores caffeine content per gram of beans and per espresso shot.
public sealed record CoffeeCaffeineProfile(double CaffeineMgPerGram, double CaffeineMgPerShot);

// Stores espresso shot counts for the standard serving sizes.
public sealed record CoffeeShopRecipeProfile(double SmallShots, double MediumShots, double LargeShots)
{
    // Returns the espresso shot count for the selected serving size.
    public double GetShots(ServingSize size) => size switch
    {
        ServingSize.Small => SmallShots,
        ServingSize.Medium => MediumShots,
        ServingSize.Large => LargeShots,
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
}

// Practical estimates for the quiz's limited inputs, not measurements of a specific drink.
public static class CaffeineCalculationConfig
{
    public const double EspressoShotMl = 30;
    public const double MinimumShots = 0.5;
    public const double MaximumShots = 4;
    public const int EstimateRoundingMg = 5;

    public static FrozenDictionary<CoffeeBeanType, CoffeeCaffeineProfile> Beans { get; } =
        new Dictionary<CoffeeBeanType, CoffeeCaffeineProfile>
        {
            // Dry bean baselines of 1.35%/2.45%; café shots use separate average recipes.
            [CoffeeBeanType.Arabica] = new(13.5, 60),
            [CoffeeBeanType.Robusta] = new(24.5, 100)
        }.ToFrozenDictionary();

    public static FrozenDictionary<CoffeeBrewingMethod, double> Extraction { get; } =
        new Dictionary<CoffeeBrewingMethod, double>
        {
            [CoffeeBrewingMethod.EspressoMachine] = 0.58, // Short contact; 8 g Arabica yields about 63 mg.
            [CoffeeBrewingMethod.FrenchPress] = 0.95, // Long hot immersion extracts most available caffeine.
            [CoffeeBrewingMethod.PourOver] = 0.90, // Typical hot filter extraction.
            [CoffeeBrewingMethod.MokaPot] = 0.85, // Short hot percolation, below complete extraction.
            [CoffeeBrewingMethod.Turkish] = 0.90, // Fine grind and hot immersion.
            [CoffeeBrewingMethod.ColdBrew] = 0.90, // Long contact does not create more caffeine than the dose holds.
            [CoffeeBrewingMethod.Kettle] = 0.90 // Simple immersion; remains a distinct method.
        }.ToFrozenDictionary();

    public static FrozenDictionary<CoffeeDrinkType, CoffeeShopRecipeProfile> ShopRecipes { get; } =
        new Dictionary<CoffeeDrinkType, CoffeeShopRecipeProfile>
        {
            [CoffeeDrinkType.Espresso] = new(0.5, 1, 2), // Existing 15/30/60 ml outputs.
            [CoffeeDrinkType.Macchiato] = new(1, 1, 2), // Small milk addition to an espresso base.
            [CoffeeDrinkType.Americano] = new(1, 2, 3), // Larger recipes commonly add shots as well as water.
            [CoffeeDrinkType.Cappuccino] = new(1, 2, 2), // Extra milk at large size need not add a shot.
            [CoffeeDrinkType.Latte] = new(1, 2, 2), // Medium and large share a double espresso base.
            [CoffeeDrinkType.FlatWhite] = new(2, 2, 3), // Typically starts with a double espresso.
            [CoffeeDrinkType.Mocha] = new(1, 2, 2), // Espresso contribution; chocolate quantity is unknown.
            [CoffeeDrinkType.Affogato] = new(1, 1, 2) // Ice cream changes volume, not espresso caffeine.
        }.ToFrozenDictionary();

    // Extracted caffeine per gram of dry leaf for typical preparation, not total leaf caffeine.
    public static FrozenDictionary<TeaType, double> TeaMgPerGram { get; } =
        new Dictionary<TeaType, double>
        {
            [TeaType.Black] = 20, // 2 g gives a typical 40 mg serving.
            [TeaType.Green] = 15 // 2 g gives a typical 30 mg serving.
        }.ToFrozenDictionary();
}
