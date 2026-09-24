namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeBeanTypeStep : ContentView
{
    // Initializes the coffee bean type step.
    public CoffeeBeanTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeBeanType, InitializeComponent);
}
