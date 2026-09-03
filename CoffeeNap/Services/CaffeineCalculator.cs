using CoffeeNap.Models;

namespace CoffeeNap.Services;

public interface ICaffeineCalculator
{
    ConsumptionCalculationResult Calculate(AddConsumptionQuizState state);
}

// Примерная оценка кофеина
public sealed class CaffeineCalculator : ICaffeineCalculator
{
    private const double ArabicaMultiplier = 1.0;
    private const double RobustaMultiplier = 1.7;
    private const double ArabicaCaffeinePerGram = 12.0;

    // Публичная функция для подсчёта
    public ConsumptionCalculationResult Calculate(AddConsumptionQuizState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.CoffeeLocation switch
        {
            CoffeeLocation.Home => CalculateHome(state),
            CoffeeLocation.Outside => CalculateOutside(state),
            _ => throw new InvalidOperationException("Coffee location is required.")
        };
    }

    // Подсчёт кофе вне дома
    private static ConsumptionCalculationResult CalculateOutside(AddConsumptionQuizState state)
    {
        if (state.CoffeeDrinkType is not { } drink ||
            state.VolumeMl is not > 0 ||
            state.BeanType is not { } bean)
        {
            throw new InvalidOperationException("Outside coffee answers are incomplete.");
        }

        var caffeine = (int)Math.Round(
            GetCaffeinePer100Ml(drink) * state.VolumeMl.Value / 100d * GetBeanMultiplier(bean));
        var drinkName = CoffeeQuizCatalog.GetDrinkDisplay(drink);
        return new ConsumptionCalculationResult(
            Math.Max(1, caffeine),
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

    // Подсчёт кофе дома
    private static ConsumptionCalculationResult CalculateHome(AddConsumptionQuizState state)
    {
        if (state.BrewingMethod is not { } method ||
            state.CoffeeAmountGrams is not > 0 ||
            state.BeanType is not { } bean)
        {
            throw new InvalidOperationException("Home coffee answers are incomplete.");
        }

        var caffeine = (int)Math.Round(
            state.CoffeeAmountGrams.Value *
            ArabicaCaffeinePerGram *
            GetBeanMultiplier(bean) *
            GetExtractionCoefficient(method));
        return new ConsumptionCalculationResult(
            Math.Max(1, caffeine),
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

    private static double GetBeanMultiplier(CoffeeBeanType bean) => bean switch
    {
        CoffeeBeanType.Arabica => ArabicaMultiplier,
        CoffeeBeanType.Robusta => RobustaMultiplier,
        _ => throw new ArgumentOutOfRangeException(nameof(bean))
    };

    // Кол-во кофеина в напитках на 100 мл
    private static double GetCaffeinePer100Ml(CoffeeDrinkType drink) => drink switch
    {
        CoffeeDrinkType.Espresso => 212,
        CoffeeDrinkType.Macchiato => 100,
        CoffeeDrinkType.Americano => 45,
        CoffeeDrinkType.Cappuccino => 40,
        CoffeeDrinkType.Latte => 32,
        CoffeeDrinkType.FlatWhite => 55,
        CoffeeDrinkType.Mocha => 38,
        CoffeeDrinkType.Affogato => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(drink))
    };

    // Коэфициент типов приготовления кофе
    private static double GetExtractionCoefficient(CoffeeBrewingMethod method) => method switch
    {
        CoffeeBrewingMethod.EspressoMachine => 0.75,
        CoffeeBrewingMethod.ColdBrew => 0.80,
        CoffeeBrewingMethod.MokaPot => 0.70,
        CoffeeBrewingMethod.FrenchPress => 0.75,
        CoffeeBrewingMethod.Turkish => 0.85,
        CoffeeBrewingMethod.PourOver => 0.70,
        CoffeeBrewingMethod.Kettle => 0.75,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}
