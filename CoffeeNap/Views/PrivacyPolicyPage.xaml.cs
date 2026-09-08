using CoffeeNap.Services;

namespace CoffeeNap.Views;

public partial class PrivacyPolicyPage : ContentPage
{
    private readonly IAppNavigationService _navigationService;

    public PrivacyPolicyPage(IAppNavigationService navigationService)
    {
        InitializeComponent();

        _navigationService = navigationService;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnBackClicked(object? sender, EventArgs eventArgs)
        => await _navigationService.GoBackAsync();
}
