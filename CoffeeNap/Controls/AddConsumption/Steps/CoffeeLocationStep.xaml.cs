namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeLocationStep : ContentView
{
    // Initializes the coffee location step.
    public CoffeeLocationStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeLocation, InitializeComponent);
}
