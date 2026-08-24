using CoffeeNap.ViewModels;

namespace CoffeeNap;

public partial class WelcomePage : ContentPage
{
    private bool isStarting;

    public WelcomePage()
    {
        InitializeComponent();
        BindingContext = new WelcomeViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WelcomePageContent.TranslationX = 0;
        WelcomePageContent.Opacity = 1;
        isStarting = false;
    }

    private async void OnStartButtonClicked(object? sender, EventArgs e)
    {
        if (isStarting || BindingContext is not WelcomeViewModel viewModel)
        {
            return;
        }

        isStarting = true;
        try
        {
            // 100 ms выхода + 180 ms входа NameSetupPage дают переход около 280 ms.
            await Task.WhenAll(
                WelcomePageContent.TranslateToAsync(-48, 0, 100, Easing.CubicIn),
                WelcomePageContent.FadeToAsync(0.25, 100, Easing.CubicIn));

            if (viewModel.StartCommand.CanExecute(null))
            {
                await viewModel.StartCommand.ExecuteAsync(null);
            }
        }
        finally
        {
            isStarting = false;
        }
    }
}
