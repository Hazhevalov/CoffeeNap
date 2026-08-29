namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeBeanTypeStep : ContentView
{
    public CoffeeBeanTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeBeanType, InitializeComponent);
}
