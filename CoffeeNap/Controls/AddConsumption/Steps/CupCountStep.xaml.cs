namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CupCountStep : ContentView
{
    public CupCountStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CupCount, InitializeComponent);
}
