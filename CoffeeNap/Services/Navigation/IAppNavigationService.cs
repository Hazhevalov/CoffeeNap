namespace CoffeeNap.Services;

using CoffeeNap.ViewModels;

public interface IAppNavigationService
{
    // Opens the settings page.
    Task OpenSettingsAsync();
    // Opens the privacy policy page.
    Task OpenPrivacyPolicyAsync();
    // Navigates back to the previous page.
    Task GoBackAsync();
    // Navigates to the requested top-level route.
    Task NavigateToTopLevelAsync(
        string absoluteRoute,
        BottomNavigationViewModel? sourceNavigation = null);
}
