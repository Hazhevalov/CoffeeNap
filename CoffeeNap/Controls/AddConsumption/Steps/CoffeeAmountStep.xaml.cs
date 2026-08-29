namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeAmountStep : ContentView
{
    public CoffeeAmountStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeAmount, InitializeComponent);
}
