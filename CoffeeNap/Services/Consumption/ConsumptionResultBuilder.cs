using CoffeeNap.Models;

namespace CoffeeNap.Services;

// Formats the calculated snapshot; formulas belong to CaffeineEstimate.
internal static class ConsumptionResultBuilder
{
    public static ConsumptionCalculationResult Build(AddConsumptionQuizState state, int caffeine)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.DrinkType switch
        {
            CaffeineConsumptionType.Coffee => BuildCoffee(state, caffeine),
            CaffeineConsumptionType.Tea => BuildTea(state, caffeine),
            CaffeineConsumptionType.EnergyDrink => BuildEnergyDrink(state, caffeine),
            _ => throw new InvalidOperationException("Drink type is required.")
        };
    }

    private static ConsumptionCalculationResult BuildCoffee(AddConsumptionQuizState state, int caffeine) =>
        state.CoffeeLocation switch
        {
            CoffeeLocation.Home => BuildHome(state, caffeine),
            CoffeeLocation.Outside => BuildOutside(state, caffeine),
            _ => throw new InvalidOperationException("Coffee location is required.")
        };

    private static ConsumptionCalculationResult BuildTea(AddConsumptionQuizState state, int caffeine)
    {
        if (state.TeaType is not { } teaType || state.TeaAmountGrams is not > 0)
        {
            throw new InvalidOperationException("Tea answers are incomplete.");
        }

        var teaTypeDisplay = TeaQuizCatalog.GetTypeDisplay(teaType);
        var gramsDisplay = $"{state.TeaAmountGrams:0.#} {LocalizationService.Current["GramShort"]}";
        var amountDisplay = state.TeaAmountDisplay ?? gramsDisplay;
        var amountValue = string.Equals(amountDisplay, gramsDisplay, StringComparison.Ordinal)
            ? string.Empty
            : gramsDisplay;

        return new ConsumptionCalculationResult(
            caffeine,
            TeaQuizCatalog.GetDrinkDisplay(teaType),
            CaffeineConsumptionType.Tea,
            LocalizationService.Current["Tea"],
            teaTypeDisplay,
            amountDisplay,
            amountValue,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ConsumptionCalculationResult BuildEnergyDrink(AddConsumptionQuizState state, int caffeine)
    {
        if (state.EnergyDrinkVolumeMl is not > 0)
        {
            throw new InvalidOperationException("Energy drink volume is required.");
        }

        var volumeMl = state.EnergyDrinkVolumeMl.Value;
        var volumeDisplay = $"{volumeMl} {LocalizationService.Current["MilliliterShort"]}";

        return new ConsumptionCalculationResult(
            caffeine,
            LocalizationService.Current["EnergyDrink"],
            CaffeineConsumptionType.EnergyDrink,
            LocalizationService.Current["EnergyDrink"],
            volumeDisplay,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    // Coffee outside display.
    private static ConsumptionCalculationResult BuildOutside(AddConsumptionQuizState state, int caffeine)
    {
        if (state.CoffeeDrinkType is not { } drink ||
            state.VolumeMl is not > 0 ||
            state.BeanType is not { } bean)
        {
            throw new InvalidOperationException("Outside coffee answers are incomplete.");
        }

        var drinkName = CoffeeQuizCatalog.GetDrinkDisplay(drink);
        return new ConsumptionCalculationResult(
            caffeine,
            drinkName,
            CaffeineConsumptionType.Coffee,
            LocalizationService.Current["OutsideCoffee"],
            CoffeeQuizCatalog.GetLocationDisplay(CoffeeLocation.Outside),
            drinkName,
            string.Empty,
            state.VolumeDisplay ?? $"{state.VolumeMl} {LocalizationService.Current["MilliliterShort"]}",
            CoffeeQuizCatalog.GetBeanDisplay(bean),
            $"{state.VolumeMl} {LocalizationService.Current["MilliliterShort"]}");
    }

    // Home coffee display.
    private static ConsumptionCalculationResult BuildHome(AddConsumptionQuizState state, int caffeine)
    {
        if (state.BrewingMethod is not { } method ||
            state.CoffeeAmountGrams is not > 0 ||
            state.BeanType is not { } bean)
        {
            throw new InvalidOperationException("Home coffee answers are incomplete.");
        }

        return new ConsumptionCalculationResult(
            caffeine,
            LocalizationService.Current["HomeCoffee"],
            CaffeineConsumptionType.Coffee,
            LocalizationService.Current["HomeCoffee"],
            CoffeeQuizCatalog.GetBrewingMethodDisplay(method),
            state.CoffeeAmountDisplay ?? $"{state.CoffeeAmountGrams:0.#} {LocalizationService.Current["GramShort"]}",
            $"{state.CoffeeAmountGrams:0.#} {LocalizationService.Current["GramShort"]}",
            CoffeeQuizCatalog.GetBeanDisplay(bean),
            string.Empty,
            string.Empty);
    }

}
