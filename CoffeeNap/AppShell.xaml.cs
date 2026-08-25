using CoffeeNap.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeNap;

/// <summary>Shell создаётся только после чтения startup profile из SQLite.</summary>
public partial class AppShell : Shell
{
    public AppShell(AppStartupState startupState, IServiceProvider services)
    {
        InitializeComponent();
        OnboardingShellContent.ContentTemplate = new DataTemplate(
            () => services.GetRequiredService<OnboardingPage>());
        MainShellContent.ContentTemplate = new DataTemplate(
            () => services.GetRequiredService<MainPage>());

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AddConsumptionPage), typeof(AddConsumptionPage));
        Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));

        var profile = startupState.UserProfile;
        var hasName = !string.IsNullOrWhiteSpace(profile.UserName);
        CurrentItem = profile.OnboardingCompleted && hasName
            ? MainShellItem
            : OnboardingShellItem;
    }
}
