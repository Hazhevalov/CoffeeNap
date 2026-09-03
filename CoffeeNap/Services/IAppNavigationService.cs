namespace CoffeeNap.Services;

public interface IAppNavigationService
{
    Task OpenSettingsAsync();
    Task OpenPrivacyPolicyAsync();
    Task NavigateToTopLevelAsync(string absoluteRoute);
}
