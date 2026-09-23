namespace CoffeeNap.Models;

/// <summary>Shared input limits for the form, calculator and persistence boundary.</summary>
public static class ConsumptionRecipeValidator
{
    public const double MaximumCoffeeGrams = 100;
    public const double MaximumTeaGrams = 50;
    public const int MaximumVolumeMl = 2000;
    public const int MaximumCaffeineMg = 3000;

    public static bool IsAmountValid(double? value, double maximum) =>
        value is { } amount && double.IsFinite(amount) && amount > 0 && amount <= maximum;

    private static bool Defined<T>(T? value) where T : struct, Enum =>
        value is { } item && Enum.IsDefined(item);

    public static bool IsValid(AddConsumptionQuizState state) =>
        state.DrinkType is not null && IsValid(ConsumptionRecipeMapper.CreateFrom(state));

    public static bool IsValid(LastConsumptionRecipe recipe) => recipe.DrinkType switch
    {
        CaffeineConsumptionType.Coffee => recipe.CoffeeLocation switch
        {
            CoffeeLocation.Home => Defined(recipe.BrewingMethod) && Defined(recipe.BeanType) &&
                IsAmountValid(recipe.CoffeeAmountGrams, MaximumCoffeeGrams),
            CoffeeLocation.Outside => Defined(recipe.CoffeeDrinkType) && Defined(recipe.BeanType) &&
                recipe.VolumeMl is > 0 and <= MaximumVolumeMl &&
                (recipe.ServingSize is null || Defined(recipe.ServingSize)),
            _ => false
        },
        CaffeineConsumptionType.Tea => Defined(recipe.TeaType) &&
            IsAmountValid(recipe.TeaAmountGrams, MaximumTeaGrams),
        CaffeineConsumptionType.EnergyDrink => recipe.EnergyDrinkVolumeMl is > 0 and <= MaximumVolumeMl,
        _ => false
    };
}
