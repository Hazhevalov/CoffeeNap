namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeDrinkTypeStep : ContentView
{
    public CoffeeDrinkTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeDrinkType, InitializeComponent);
}
