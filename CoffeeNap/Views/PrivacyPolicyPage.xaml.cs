using CoffeeNap.Services;

namespace CoffeeNap.Views;

public partial class PrivacyPolicyPage : ContentPage
{
    private readonly IAppNavigationService _navigationService;

    // Initializes the privacy policy page.
    public PrivacyPolicyPage(IAppNavigationService navigationService)
    {
        InitializeComponent();

        _navigationService = navigationService;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    // Navigates back from the privacy policy page.
    private async void OnBackClicked(object? sender, EventArgs eventArgs)
        => await _navigationService.GoBackAsync();
}
