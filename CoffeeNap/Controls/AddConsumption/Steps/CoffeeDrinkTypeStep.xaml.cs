namespace CoffeeNap.Controls.AddConsumption.Steps;

using CoffeeNap.Models;

public partial class CoffeeDrinkTypeStep : ContentView
{
    public CoffeeDrinkTypeStep() =>
        LazyStepLoader.LoadWhenCurrent(this, AddConsumptionStep.CoffeeDrinkType, InitializeComponent);
        private async void OnBorderTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            await border.ScaleToAsync(
                0.97,
                70,
                Easing.CubicOut);

            await border.ScaleToAsync(
                1.0,
                70,
                Easing.CubicIn);
        }
    }
}
