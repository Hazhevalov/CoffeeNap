namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeAmountStep : ContentView
{
    // Initializes the coffee amount step.
    public CoffeeAmountStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeAmount, InitializeComponent);
}
