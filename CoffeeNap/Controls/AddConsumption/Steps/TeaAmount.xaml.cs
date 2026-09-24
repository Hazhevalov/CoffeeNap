using CoffeeNap.Models;

namespace CoffeeNap.Controls.AddConsumption.Steps;

public partial class TeaAmount : ContentView
{
    // Initializes the tea amount.
    public TeaAmount() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.TeaAmount, InitializeComponent);
}
