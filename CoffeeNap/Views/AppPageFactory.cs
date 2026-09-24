using Microsoft.Extensions.DependencyInjection;

namespace CoffeeNap.Views;

public sealed class AppPageFactory
{
    private readonly IServiceProvider _services;

    // Initializes the app page factory.
    public AppPageFactory(IServiceProvider services)
    {
        _services = services;
    }

    // Creates the main page with its dependencies.
    public MainPage CreateMainPage() => _services.GetRequiredService<MainPage>();

    // Creates the add-consumption page with its dependencies.
    public AddConsumptionPage CreateAddConsumptionPage() =>
        _services.GetRequiredService<AddConsumptionPage>();

    // Creates the calendar page with its dependencies.
    public CalendarPage CreateCalendarPage() =>
        _services.GetRequiredService<CalendarPage>();

    // Creates the onboarding page with its dependencies.
    public OnboardingPage CreateOnboardingPage() => _services.GetRequiredService<OnboardingPage>();

    // Creates the tab host with its dependencies.
    public TabHostPage CreateTabHostPage() => _services.GetRequiredService<TabHostPage>();
}
