namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class BrewingMethodStep : ContentView
{
    // Initializes the brewing method step.
    public BrewingMethodStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.BrewingMethod, InitializeComponent);
}
