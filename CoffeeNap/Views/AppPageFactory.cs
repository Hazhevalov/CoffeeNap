using CoffeeNap.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeNap.Views;

public sealed class AppPageFactory
{
    private readonly IServiceProvider _services;

    public AppPageFactory(IServiceProvider services)
    {
        _services = services;
    }

    public MainPage CreateMainPage() => _services.GetRequiredService<MainPage>();

    public OnboardingPage CreateOnboardingPage() => _services.GetRequiredService<OnboardingPage>();
}
