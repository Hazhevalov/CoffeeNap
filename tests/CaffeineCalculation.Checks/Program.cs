using CoffeeNap.Models;
using CoffeeNap.Services;

var checks = 0;
// Records whether a test condition passes.
void Check(bool ok, string name) { checks++; if (!ok) throw new Exception(name); }
// Checks that a calculated caffeine amount matches the expected value.
void Equal(double expected, double actual, string name) => Check(Math.Abs(expected - actual) < 1e-8, $"{name}: expected {expected}, got {actual}");
var calculator = new CaffeineCalculator();
// Creates home coffee quiz answers for calculation checks.
AddConsumptionQuizState Home(double grams, CoffeeBeanType bean, CoffeeBrewingMethod method) => new()
{
    DrinkType = CaffeineConsumptionType.Coffee, CoffeeLocation = CoffeeLocation.Home,
    CoffeeAmountGrams = grams, BeanType = bean, BrewingMethod = method
};
var homeCases = new[] {
    (8d, CoffeeBeanType.Arabica, CoffeeBrewingMethod.EspressoMachine, 65),
    (8d, CoffeeBeanType.Robusta, CoffeeBrewingMethod.EspressoMachine, 115),
    (15d, CoffeeBeanType.Arabica, CoffeeBrewingMethod.FrenchPress, 190),
    (15d, CoffeeBeanType.Arabica, CoffeeBrewingMethod.PourOver, 180),
    (15d, CoffeeBeanType.Arabica, CoffeeBrewingMethod.Kettle, 180),
    (15d, CoffeeBeanType.Arabica, CoffeeBrewingMethod.ColdBrew, 180)
};
foreach (var (grams, bean, method, expected) in homeCases)
{
    var state = Home(grams, bean, method);
    Equal(expected, calculator.Calculate(state).CaffeineMg, method.ToString());
    var restored = new AddConsumptionQuizState();
    ConsumptionRecipeMapper.ApplyTo(ConsumptionRecipeMapper.CreateFrom(state), restored);
    Equal(expected, calculator.Calculate(restored).CaffeineMg, "Last Recipe home");
}
foreach (var method in Enum.GetValues<CoffeeBrewingMethod>())
{
    Check(CaffeineCalculationConfig.Extraction[method] is > 0 and <= 1, "Extraction range");
    Check(CaffeineEstimate.Home(10, CoffeeBeanType.Robusta, method) > CaffeineEstimate.Home(10, CoffeeBeanType.Arabica, method), "Bean ordering");
    foreach (var bean in Enum.GetValues<CoffeeBeanType>())
    {
        var previous = 0;
        for (var i = 1; i <= 3000; i++)
        {
            var grams = i / 100d;
            var value = CaffeineEstimate.Home(grams, bean, method);
            Check(value >= previous && value <= grams * CaffeineCalculationConfig.Beans[bean].CaffeineMgPerGram, "Home monotonicity/physical bound");
            previous = value;
        }
    }
}
var expectedShots = new Dictionary<CoffeeDrinkType, double[]> {
    [CoffeeDrinkType.Espresso] = [0.5, 1, 2], [CoffeeDrinkType.Macchiato] = [1, 1, 2],
    [CoffeeDrinkType.Americano] = [1, 2, 3], [CoffeeDrinkType.Cappuccino] = [1, 2, 2],
    [CoffeeDrinkType.Latte] = [1, 2, 2], [CoffeeDrinkType.FlatWhite] = [2, 2, 3],
    [CoffeeDrinkType.Mocha] = [1, 2, 2], [CoffeeDrinkType.Affogato] = [1, 1, 2]
};
foreach (var drink in Enum.GetValues<CoffeeDrinkType>())
{
    foreach (var size in Enum.GetValues<ServingSize>())
    {
        var volume = CoffeeServingCatalog.GetVolumeMl(drink, size);
        foreach (var bean in Enum.GetValues<CoffeeBeanType>())
        {
            var expected = expectedShots[drink][(int)size] * (bean == CoffeeBeanType.Arabica ? 60 : 100);
            var state = new AddConsumptionQuizState { DrinkType = CaffeineConsumptionType.Coffee,
                CoffeeLocation = CoffeeLocation.Outside, CoffeeDrinkType = drink, BeanType = bean,
                ServingSize = size, VolumeMl = volume };
            Equal(expected, calculator.Calculate(state).CaffeineMg, "Outside preset");
            state.ServingSize = null;
            Equal(expected, calculator.Calculate(state).CaffeineMg, "Manual preset anchor");
            var restored = new AddConsumptionQuizState();
            ConsumptionRecipeMapper.ApplyTo(ConsumptionRecipeMapper.CreateFrom(state), restored);
            Equal(expected, calculator.Calculate(restored).CaffeineMg, "Last Recipe outside");
        }
        Check(Math.Abs(CaffeineEstimate.GetShotEquivalent(drink, volume - 0.001) - CaffeineEstimate.GetShotEquivalent(drink, volume + 0.001)) < 0.001, "Continuity at preset");
    }
    var previous = 0d;
    for (var volume = 1; volume <= 2000; volume++)
    {
        var shots = CaffeineEstimate.GetShotEquivalent(drink, volume);
        Check(shots >= previous && shots is >= 0.5 and <= 4, "Shot monotonicity/clamp");
        previous = shots;
    }
}
Equal(1.5, CaffeineEstimate.GetShotEquivalent(CoffeeDrinkType.Americano, 200), "Americano midpoint using actual 150/250 ml profile");
Equal(120, CaffeineEstimate.Outside(CoffeeDrinkType.Latte, CoffeeBeanType.Arabica, 300, ServingSize.Medium), "Latte");
Equal(120, CaffeineEstimate.Outside(CoffeeDrinkType.Latte, CoffeeBeanType.Arabica, 400, ServingSize.Large), "Milk adds no caffeine");
foreach (var tea in Enum.GetValues<TeaType>())
{
    for (var spoons = 1; spoons <= 3; spoons++)
    {
        var state = new AddConsumptionQuizState { DrinkType = CaffeineConsumptionType.Tea,
            TeaType = tea, TeaAmountGrams = TeaQuizCatalog.GetSpoonGrams(spoons), TeaSpoonCount = spoons };
        var expected = spoons * (tea == TeaType.Black ? 40 : 30);
        Equal(expected, calculator.Calculate(state).CaffeineMg, "Tea spoons");
        var restored = new AddConsumptionQuizState();
        ConsumptionRecipeMapper.ApplyTo(ConsumptionRecipeMapper.CreateFrom(state), restored);
        Equal(expected, calculator.Calculate(restored).CaffeineMg, "Last Recipe tea");
    }
    Check(CaffeineEstimate.Tea(4, tea) > CaffeineEstimate.Tea(2, tea), "Tea monotonicity");
}
Equal(100, CaffeineEstimate.Tea(5, TeaType.Black), "Manual black tea");
Equal(106, calculator.Calculate(new() { DrinkType = CaffeineConsumptionType.EnergyDrink, EnergyDrinkVolumeMl = 330 }).CaffeineMg, "Energy unchanged");
foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1, 0, double.MaxValue })
{
    foreach (Action action in new Action[] { () => CaffeineEstimate.Home(invalid, CoffeeBeanType.Arabica, CoffeeBrewingMethod.Kettle), () => CaffeineEstimate.Tea(invalid, TeaType.Black) })
    {
        var rejected = false;
        try { action(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "Invalid/overflow amount rejected");
    }
}
Console.WriteLine($"PASS: {checks} checks, including all profiles, interpolation, bounds, facade and Last Recipe round trips.");
