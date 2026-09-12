using CoffeeNap.Models;
using CoffeeNap.Services;

// LocalizationService owns process-wide culture and a static Current instance.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CoffeeNap.Tests;

public sealed class ConsumptionTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    public void RecipesRoundTripAndKeepCalculatedResults(string language)
    {
        var localization = new LocalizationService(new UnusedDataService());
        localization.PrepareSavedLanguageForRestart(language);
        var calculator = new CaffeineCalculator();
        var cases = new (AddConsumptionQuizState State, int ExpectedMg)[]
        {
            (new() { DrinkType = CaffeineConsumptionType.Coffee, CoffeeLocation = CoffeeLocation.Home,
                BrewingMethod = CoffeeBrewingMethod.FrenchPress, CoffeeAmountGrams = 16,
                CoffeeSpoonCount = 2, BeanType = CoffeeBeanType.Arabica }, 144),
            (new() { DrinkType = CaffeineConsumptionType.Coffee, CoffeeLocation = CoffeeLocation.Outside,
                CoffeeDrinkType = CoffeeDrinkType.Latte, VolumeMl = 300,
                ServingSize = ServingSize.Medium, BeanType = CoffeeBeanType.Robusta }, 163),
            (new() { DrinkType = CaffeineConsumptionType.Tea, TeaType = TeaType.Black,
                TeaAmountGrams = 5, TeaSpoonCount = 2 }, 30),
            (new() { DrinkType = CaffeineConsumptionType.Tea, TeaType = TeaType.Green,
                TeaAmountGrams = 2.5 }, 12),
            (new() { DrinkType = CaffeineConsumptionType.EnergyDrink, EnergyDrinkVolumeMl = 250 }, 80),
            (new() { DrinkType = CaffeineConsumptionType.EnergyDrink, EnergyDrinkVolumeMl = 330 }, 106),
            (new() { DrinkType = CaffeineConsumptionType.EnergyDrink, EnergyDrinkVolumeMl = 500 }, 160)
        };

        foreach (var (state, expectedMg) in cases)
        {
            var result = calculator.Calculate(state);
            Assert.Equal(expectedMg, result.CaffeineMg);
            var recipe = ConsumptionRecipeMapper.CreateFrom(state);
            var restored = FullyAnsweredState();
            ConsumptionRecipeMapper.ApplyTo(recipe, restored);
            var recalculated = calculator.Calculate(restored);
            Assert.Equal(result.CaffeineMg, recalculated.CaffeineMg);
            Assert.Equal(result.Type, recalculated.Type);
            Assert.Equal(result.DisplayName, recalculated.DisplayName);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(recipe),
                System.Text.Json.JsonSerializer.Serialize(ConsumptionRecipeMapper.CreateFrom(restored)));
        }
    }

    [Fact]
    public void ResetClearsEveryAnswer()
    {
        var state = FullyAnsweredState();
        state.Reset();
        Assert.All(typeof(AddConsumptionQuizState).GetProperties(), property => Assert.Null(property.GetValue(state)));
    }

    [Fact]
    public void ChangingDrinkClearsBranchesButKeepsSelectedDrink()
    {
        var state = FullyAnsweredState();
        state.ClearAfterDrinkType();
        Assert.Equal(CaffeineConsumptionType.Coffee, state.DrinkType);
        Assert.All(typeof(AddConsumptionQuizState).GetProperties().Where(p => p.Name != nameof(state.DrinkType)),
            property => Assert.Null(property.GetValue(state)));
    }

    [Fact]
    public void IncompleteAnswersCannotBeCalculatedOrSavedAsRecipe()
    {
        var calculator = new CaffeineCalculator();
        Assert.Throws<ArgumentNullException>(() => calculator.Calculate(null!));
        Assert.Throws<InvalidOperationException>(() => ConsumptionRecipeMapper.CreateFrom(new()));
        foreach (var type in Enum.GetValues<CaffeineConsumptionType>())
        {
            Assert.Throws<InvalidOperationException>(() => calculator.Calculate(new() { DrinkType = type }));
        }
    }

    [Theory]
    [InlineData(CoffeeDrinkType.Espresso, 15, 30, 60)]
    [InlineData(CoffeeDrinkType.Macchiato, 30, 60, 90)]
    [InlineData(CoffeeDrinkType.Americano, 150, 250, 350)]
    [InlineData(CoffeeDrinkType.Cappuccino, 150, 250, 350)]
    [InlineData(CoffeeDrinkType.Latte, 200, 300, 400)]
    [InlineData(CoffeeDrinkType.FlatWhite, 150, 200, 250)]
    [InlineData(CoffeeDrinkType.Mocha, 200, 300, 400)]
    [InlineData(CoffeeDrinkType.Affogato, 60, 90, 120)]
    public void ServingVolumesRemainStable(CoffeeDrinkType type, int small, int medium, int large)
    {
        Assert.Equal(small, CoffeeServingCatalog.GetVolumeMl(type, ServingSize.Small));
        Assert.Equal(medium, CoffeeServingCatalog.GetVolumeMl(type, ServingSize.Medium));
        Assert.Equal(large, CoffeeServingCatalog.GetVolumeMl(type, ServingSize.Large));
    }

    private static AddConsumptionQuizState FullyAnsweredState() => new()
    {
        DrinkType = CaffeineConsumptionType.Coffee,
        CoffeeLocation = CoffeeLocation.Home,
        CoffeeDrinkType = CoffeeDrinkType.Espresso,
        BrewingMethod = CoffeeBrewingMethod.FrenchPress,
        ServingSize = ServingSize.Small,
        VolumeMl = 30,
        VolumeDisplay = "30 ml",
        CoffeeAmountGrams = 8,
        CoffeeSpoonCount = 1,
        CoffeeAmountDisplay = "1 spoon",
        BeanType = CoffeeBeanType.Arabica,
        TeaType = TeaType.Black,
        TeaAmountGrams = 2.5,
        TeaSpoonCount = 1,
        TeaAmountDisplay = "1 spoon",
        EnergyDrinkVolumeMl = 250
    };

    // These tests exercise real localization resources without opening a user database.
    private sealed class UnusedDataService : IAppDataService
    {
        public event EventHandler<CaffeineConsumption>? ConsumptionAdded { add { } remove { } }
        public event EventHandler<CaffeineConsumption>? ConsumptionDeleted { add { } remove { } }
        public event EventHandler? UserDataDeleted { add { } remove { } }
        public Task InitializeAsync() => throw new NotSupportedException();
        public Task<UserProfile?> GetUserProfileAsync() => throw new NotSupportedException();
        public Task SaveUserProfileAsync(UserProfile profile) => throw new NotSupportedException();
        public Task<AppSettings> GetSettingsAsync() => throw new NotSupportedException();
        public Task SaveSettingsAsync(AppSettings settings) => throw new NotSupportedException();
        public Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsBetweenAsync(DateTimeOffset fromInclusive, DateTimeOffset toExclusive) => throw new NotSupportedException();
        public Task AddConsumptionAndSaveRecipeAsync(CaffeineConsumption consumption, LastConsumptionRecipe recipe) => throw new NotSupportedException();
        public Task<LastConsumptionRecipe?> GetLastConsumptionRecipeAsync() => throw new NotSupportedException();
        public Task DeleteConsumptionAsync(int id) => throw new NotSupportedException();
        public Task DeleteAllUserDataAsync() => throw new NotSupportedException();
    }
}
