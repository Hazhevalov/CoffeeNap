using CoffeeNap.Data;
using CoffeeNap.Services;

namespace CoffeeNap.Models;

/// <summary>Maps between persistent recipes and runtime quiz state in both directions.</summary>
public static class ConsumptionRecipeMapper
{
    // Copies quiz answers into a persistent recipe.
    public static LastConsumptionRecipe CreateFrom(AddConsumptionQuizState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.DrinkType is not { } drinkType)
        {
            throw new InvalidOperationException("Drink type is required to create a recipe.");
        }

        return new LastConsumptionRecipe
        {
            Id = DatabaseConstants.LastConsumptionRecipeId,
            DrinkType = drinkType,
            CoffeeLocation = state.CoffeeLocation,
            CoffeeDrinkType = state.CoffeeDrinkType,
            BrewingMethod = state.BrewingMethod,
            BeanType = state.BeanType,
            CoffeeAmountGrams = state.CoffeeAmountGrams,
            CoffeeSpoonCount = state.CoffeeSpoonCount,
            VolumeMl = state.VolumeMl,
            ServingSize = state.ServingSize,
            TeaType = state.TeaType,
            TeaAmountGrams = state.TeaAmountGrams,
            TeaSpoonCount = state.TeaSpoonCount,
            EnergyDrinkVolumeMl = state.EnergyDrinkVolumeMl
        };
    }

    // Restores quiz answers and display labels from a saved recipe.
    public static void ApplyTo(LastConsumptionRecipe recipe, AddConsumptionQuizState state)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(state);

        state.Reset();
        state.DrinkType = recipe.DrinkType;

        switch (recipe.DrinkType)
        {
            case CaffeineConsumptionType.Coffee:
                ApplyCoffee(recipe, state);
                break;
            case CaffeineConsumptionType.Tea:
                state.TeaType = recipe.TeaType;
                state.TeaAmountGrams = recipe.TeaAmountGrams;
                state.TeaSpoonCount = recipe.TeaSpoonCount;
                state.TeaAmountDisplay = BuildAmountDisplay(
                    recipe.TeaAmountGrams,
                    recipe.TeaSpoonCount);
                break;
            case CaffeineConsumptionType.EnergyDrink:
                state.EnergyDrinkVolumeMl = recipe.EnergyDrinkVolumeMl;
                break;
            default:
                throw new InvalidOperationException("The saved recipe has an unsupported drink type.");
        }
    }

    // Restores coffee-specific quiz answers from a recipe.
    private static void ApplyCoffee(LastConsumptionRecipe recipe, AddConsumptionQuizState state)
    {
        state.CoffeeLocation = recipe.CoffeeLocation;
        state.BeanType = recipe.BeanType;

        if (recipe.CoffeeLocation == CoffeeLocation.Home)
        {
            state.BrewingMethod = recipe.BrewingMethod;
            state.CoffeeAmountGrams = recipe.CoffeeAmountGrams;
            state.CoffeeSpoonCount = recipe.CoffeeSpoonCount;
            state.CoffeeAmountDisplay = BuildAmountDisplay(
                recipe.CoffeeAmountGrams,
                recipe.CoffeeSpoonCount);
            return;
        }

        state.CoffeeDrinkType = recipe.CoffeeDrinkType;
        state.VolumeMl = recipe.VolumeMl;
        state.ServingSize = recipe.ServingSize;
        state.VolumeDisplay = recipe.ServingSize is { } servingSize
            ? CoffeeQuizCatalog.GetServingSizeDisplay(servingSize)
            : BuildVolumeDisplay(recipe.VolumeMl);
    }

    // Builds the display label for the saved ingredient amount.
    private static string? BuildAmountDisplay(double? grams, int? spoonCount)
    {
        if (spoonCount is > 0)
        {
            return CoffeeQuizCatalog.GetSpoonDisplay(spoonCount.Value);
        }

        return grams is > 0
            ? $"{grams:0.#} {LocalizationService.Current["GramShort"]}"
            : null;
    }

    // Builds the display label for the saved drink volume.
    private static string? BuildVolumeDisplay(int? volumeMl) => volumeMl is > 0
        ? $"{volumeMl} {LocalizationService.Current["MilliliterShort"]}"
        : null;
}
