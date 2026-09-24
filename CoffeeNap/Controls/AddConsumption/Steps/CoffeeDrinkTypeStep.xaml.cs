namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeDrinkTypeStep : ContentView
{
    // Initializes the coffee drink type step.
    public CoffeeDrinkTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeDrinkType, InitializeComponent);
}
