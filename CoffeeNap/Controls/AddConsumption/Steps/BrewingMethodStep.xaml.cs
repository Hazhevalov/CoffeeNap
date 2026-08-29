namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class BrewingMethodStep : ContentView
{
    public BrewingMethodStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.BrewingMethod, InitializeComponent);
}
