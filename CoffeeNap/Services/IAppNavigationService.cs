namespace CoffeeNap.Services;

public interface IAppNavigationService
{
    Task OpenSettingsAsync();
    Task OpenPrivacyPolicyAsync();
    Task GoBackAsync();
    Task NavigateToTopLevelAsync(string absoluteRoute);
    Task NavigateBackFromTopLevelAsync();
}
