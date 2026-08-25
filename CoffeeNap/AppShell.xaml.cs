using CoffeeNap.Models;
using CoffeeNap.Views;

namespace CoffeeNap;

/// <summary>Shell создаётся только после чтения startup profile из SQLite.</summary>
public partial class AppShell : Shell
{
    public const string MainAbsoluteRoute = "//MainPage/MainContent";

    public AppShell(
        UserProfile profile,
        OnboardingPage onboardingPage,
        MainPage mainPage)
    {
        InitializeComponent();
        OnboardingShellContent.Content = onboardingPage;
        MainShellContent.Content = mainPage;

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AddConsumptionPage), typeof(AddConsumptionPage));
        Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));

        var hasName = !string.IsNullOrWhiteSpace(profile.UserName);
        CurrentItem = profile.OnboardingCompleted && hasName
            ? MainShellItem
            : OnboardingShellItem;
    }
}
