using CoffeeNap.Models;

namespace CoffeeNap.Controls.AddConsumption.Steps;

public partial class EnergyDrinkVolume : ContentView
{
    // Initializes the energy drink volume.
    public EnergyDrinkVolume() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.EnergyDrinkVolume, InitializeComponent);
}
