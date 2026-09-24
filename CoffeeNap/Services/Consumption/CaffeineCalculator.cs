using CoffeeNap.Models;

namespace CoffeeNap.Services;

// The existing quiz facade; all arithmetic is pure and display formatting is separate.
public sealed class CaffeineCalculator : ICaffeineCalculator
{
    // Calculates the caffeine result from the current quiz answers.
    public ConsumptionCalculationResult Calculate(AddConsumptionQuizState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!ConsumptionRecipeValidator.IsValid(state))
            throw new InvalidOperationException("Drink parameters are outside the supported range.");
        var caffeine = state.DrinkType switch
        {
            CaffeineConsumptionType.Coffee when state.CoffeeLocation == CoffeeLocation.Home &&
                state.CoffeeAmountGrams is { } grams && state.BeanType is { } bean &&
                state.BrewingMethod is { } method => CaffeineEstimate.Home(grams, bean, method),
            CaffeineConsumptionType.Coffee when state.CoffeeLocation == CoffeeLocation.Outside &&
                state.CoffeeDrinkType is { } drink && state.BeanType is { } bean &&
                state.VolumeMl is { } volume => CaffeineEstimate.Outside(drink, bean, volume, state.ServingSize),
            CaffeineConsumptionType.Tea when state.TeaType is { } tea &&
                state.TeaAmountGrams is { } grams => CaffeineEstimate.Tea(grams, tea),
            // Label concentration and integer rounding for energy drinks remain unchanged.
            CaffeineConsumptionType.EnergyDrink when state.EnergyDrinkVolumeMl is > 0 =>
                Math.Max(1, (int)Math.Round(state.EnergyDrinkVolumeMl.Value *
                    EnergyDrinkQuizCatalog.CaffeineMgPer100Ml / 100d)),
            _ => throw new InvalidOperationException("Drink answers are incomplete.")
        };
        return ConsumptionResultBuilder.Build(state, caffeine);
    }
}
