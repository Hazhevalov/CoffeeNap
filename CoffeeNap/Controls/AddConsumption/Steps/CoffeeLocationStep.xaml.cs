namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeLocationStep : ContentView
{
    public CoffeeLocationStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeLocation, InitializeComponent);
}
