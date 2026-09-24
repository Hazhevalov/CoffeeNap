namespace CoffeeNap.Models;

// Standard serving volumes for cafe coffee.
public static class CoffeeServingCatalog
{
    private static readonly IReadOnlyDictionary<CoffeeDrinkType, CoffeeDrinkServingProfile> Profiles =
        new Dictionary<CoffeeDrinkType, CoffeeDrinkServingProfile>
        {
            [CoffeeDrinkType.Espresso] = new(15, 30, 60),
            [CoffeeDrinkType.Macchiato] = new(30, 60, 90),
            [CoffeeDrinkType.Americano] = new(150, 250, 350),
            [CoffeeDrinkType.Cappuccino] = new(150, 250, 350),
            [CoffeeDrinkType.Latte] = new(200, 300, 400),
            [CoffeeDrinkType.FlatWhite] = new(150, 200, 250),
            [CoffeeDrinkType.Mocha] = new(200, 300, 400),
            [CoffeeDrinkType.Affogato] = new(60, 90, 120)
        };

    // Returns the serving volumes for a coffee drink.
    public static CoffeeDrinkServingProfile GetServingProfile(CoffeeDrinkType drinkType)
    {
        if (Profiles.TryGetValue(drinkType, out var profile))
        {
            return profile;
        }

        throw new InvalidOperationException($"Serving profile is not configured for {drinkType}.");
    }

    // Returns the volume for the selected serving size.
    public static int GetVolumeMl(CoffeeDrinkType drinkType, ServingSize servingSize) =>
        GetServingProfile(drinkType).GetVolumeMl(servingSize);
}
