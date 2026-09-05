using CoffeeNap.Models;

namespace CoffeeNap.Controls.AddConsumption.Steps;

public partial class EnergyDrinkVolume : ContentView
{
    public EnergyDrinkVolume() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.EnergyDrinkVolume, InitializeComponent);
}
