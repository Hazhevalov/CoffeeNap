namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class ConsumptionResultStep : ContentView
{
    public ConsumptionResultStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.Result, InitializeComponent);
}
