using CoffeeNap.ViewModels;

namespace CoffeeNap;

public partial class NameSetupPage : ContentPage
{
    private bool isEntranceAnimationRunning;

    public NameSetupPage()
    {
        InitializeComponent();
        BindingContext = new NameSetupViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (isEntranceAnimationRunning)
        {
            return;
        }

        isEntranceAnimationRunning = true;
        NamePageContent.TranslationX = 64;
        NamePageContent.Opacity = 0;

        try
        {
            await Task.WhenAll(
                NamePageContent.TranslateToAsync(0, 0, 180, Easing.CubicOut),
                NamePageContent.FadeToAsync(1, 180, Easing.CubicOut));
        }
        finally
        {
            isEntranceAnimationRunning = false;
        }
    }
}
