using CoffeeNap.Services;
using CoffeeNap.Views;

namespace CoffeeNap;

/// <summary>Shell создаётся только после чтения startup profile из SQLite.</summary>
public partial class AppShell : Shell
{
    public const string MainAbsoluteRoute = "//MainPage/MainContent";

    public AppShell(
        UserStateService userState,
        OnboardingPage onboardingPage,
        MainPage mainPage)
    {
        InitializeComponent();
        OnboardingShellContent.Content = onboardingPage;
        MainShellContent.Content = mainPage;

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AddConsumptionPage), typeof(AddConsumptionPage));
        Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));

        CurrentItem = userState.IsOnboardingCompleted && userState.HasUserName
            ? MainShellItem
            : OnboardingShellItem;
    }
}
