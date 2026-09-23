using CoffeeNap.Models;

namespace CoffeeNap.Services;

// Pure numerical model: no localization, UI, storage or mutable application services.
public static class CaffeineEstimate
{
    public static int Home(double grams, CoffeeBeanType bean, CoffeeBrewingMethod method)
    {
        RequirePositiveFinite(grams);
        var raw = grams * CaffeineCalculationConfig.Beans[bean].CaffeineMgPerGram;
        var extracted = raw * Math.Clamp(CaffeineCalculationConfig.Extraction[method], 0, 1);
        var rounded = RoundCaffeineEstimate(extracted);
        // Rounding must not lift even tiny doses above their physical caffeine content.
        return (int)Math.Min(rounded, Math.Floor(raw));
    }

    public static int Outside(CoffeeDrinkType drink, CoffeeBeanType bean, int volumeMl, ServingSize? size)
    {
        RequirePositiveFinite(volumeMl);
        var shots = size is { } preset
            ? CaffeineCalculationConfig.ShopRecipes[drink].GetShots(preset)
            : GetShotEquivalent(drink, volumeMl);
        return RoundCaffeineEstimate(shots * CaffeineCalculationConfig.Beans[bean].CaffeineMgPerShot);
    }

    public static double GetShotEquivalent(CoffeeDrinkType drink, double volumeMl)
    {
        RequirePositiveFinite(volumeMl);
        var recipe = CaffeineCalculationConfig.ShopRecipes[drink];
        var volumes = CoffeeServingCatalog.GetServingProfile(drink);
        double shots;
        if (drink == CoffeeDrinkType.Espresso)
            shots = volumeMl / CaffeineCalculationConfig.EspressoShotMl;
        else if (volumeMl < volumes.SmallMl)
            shots = recipe.SmallShots * (volumeMl / volumes.SmallMl);
        else if (volumeMl <= volumes.MediumMl)
            shots = Interpolate(volumeMl, volumes.SmallMl, volumes.MediumMl, recipe.SmallShots, recipe.MediumShots);
        else
            // Extending the final segment preserves plateaus for milk-heavy large recipes.
            shots = Interpolate(volumeMl, volumes.MediumMl, volumes.LargeMl, recipe.MediumShots, recipe.LargeShots);
        return Math.Clamp(shots, CaffeineCalculationConfig.MinimumShots, CaffeineCalculationConfig.MaximumShots);
    }

    public static int Tea(double grams, TeaType type)
    {
        RequirePositiveFinite(grams);
        return RoundCaffeineEstimate(grams * CaffeineCalculationConfig.TeaMgPerGram[type]);
    }

    public static int RoundCaffeineEstimate(double caffeineMg)
    {
        if (!double.IsFinite(caffeineMg) || caffeineMg < 0)
            throw new InvalidOperationException("Caffeine estimate must be finite and non-negative.");
        var rounded = Math.Round(caffeineMg / CaffeineCalculationConfig.EstimateRoundingMg,
            MidpointRounding.AwayFromZero) * CaffeineCalculationConfig.EstimateRoundingMg;
        if (rounded > int.MaxValue)
            throw new InvalidOperationException("Caffeine estimate exceeds the supported range.");
        return (int)rounded;
    }

    private static double Interpolate(double x, double x0, double x1, double y0, double y1) =>
        y0 + (x - x0) / (x1 - x0) * (y1 - y0);

    private static void RequirePositiveFinite(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new InvalidOperationException("Amount must be positive and finite.");
    }
}
