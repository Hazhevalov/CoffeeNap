namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class DrinkTypeStep : ContentView
{
    public DrinkTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.DrinkType, InitializeComponent);
}
