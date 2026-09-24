namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class DrinkTypeStep : ContentView
{
    // Initializes the drink type step.
    public DrinkTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.DrinkType, InitializeComponent);
}
