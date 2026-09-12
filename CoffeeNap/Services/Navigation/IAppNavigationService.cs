namespace CoffeeNap.Services;

using CoffeeNap.ViewModels;

public interface IAppNavigationService
{
    Task OpenSettingsAsync();
    Task OpenPrivacyPolicyAsync();
    Task GoBackAsync();
    Task NavigateToTopLevelAsync(
        string absoluteRoute,
        BottomNavigationViewModel? sourceNavigation = null);
}
