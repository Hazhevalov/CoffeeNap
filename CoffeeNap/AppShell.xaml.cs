using CoffeeNap.Services;
using CoffeeNap.Views;

namespace CoffeeNap;

/// <summary>Shell создаётся только после чтения startup profile из SQLite.</summary>
public partial class AppShell : Shell
{
    public const string MainAbsoluteRoute = "//AppTabs/MainPage";
    public const string AddConsumptionAbsoluteRoute = "//AppTabs/AddConsumptionPage";
    public const string CalendarAbsoluteRoute = "//AppTabs/CalendarPage";

    public AppShell(
        UserStateService userState,
        OnboardingPage onboardingPage,
        MainPage mainPage,
        AddConsumptionPage addConsumptionPage,
        CalendarPage calendarPage)
    {
        InitializeComponent();
        OnboardingShellContent.Content = onboardingPage;
        MainShellContent.Content = mainPage;
        AddConsumptionShellContent.Content = addConsumptionPage;
        CalendarShellContent.Content = calendarPage;

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        CurrentItem = userState.IsOnboardingCompleted && userState.HasUserName
            ? MainShellItem
            : OnboardingShellItem;
    }
}
