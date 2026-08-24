namespace CoffeeNap.Services;

/// <summary>Небольшая обёртка над MAUI Preferences для данных onboarding.</summary>
public static class UserPreferencesService
{
    public static string? GetUserName()
    {
        var value = Preferences.Default.Get(PreferenceKeys.UserName, string.Empty).Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static void SetUserName(string userName) =>
        Preferences.Default.Set(PreferenceKeys.UserName, userName);

    public static bool IsOnboardingCompleted() =>
        Preferences.Default.Get(PreferenceKeys.OnboardingCompleted, false);

    public static void SetOnboardingCompleted() =>
        Preferences.Default.Set(PreferenceKeys.OnboardingCompleted, true);
}
