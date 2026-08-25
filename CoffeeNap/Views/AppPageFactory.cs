using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

public sealed class AppPageFactory
{
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly OnboardingViewModel _onboardingViewModel;

    public AppPageFactory(
        MainPageViewModel mainPageViewModel,
        OnboardingViewModel onboardingViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        _onboardingViewModel = onboardingViewModel;
    }

    public MainPage CreateMainPage() => new(_mainPageViewModel);

    public OnboardingPage CreateOnboardingPage() => new(_onboardingViewModel);
}
