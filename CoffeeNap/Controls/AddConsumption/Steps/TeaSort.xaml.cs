using CoffeeNap.Models;

namespace CoffeeNap.Controls.AddConsumption.Steps;

public partial class TeaSort : ContentView
{
    // Initializes the tea sort.
    public TeaSort() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.TeaSort, InitializeComponent);
}
