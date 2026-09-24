namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class ConsumptionResultStep : ContentView
{
    // Initializes the consumption result step.
    public ConsumptionResultStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.Result, InitializeComponent);
}
