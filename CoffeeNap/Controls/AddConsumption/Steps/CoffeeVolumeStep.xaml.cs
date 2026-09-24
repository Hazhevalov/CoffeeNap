namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeVolumeStep : ContentView
{
    // Initializes the coffee volume step.
    public CoffeeVolumeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeVolume, InitializeComponent);
}
