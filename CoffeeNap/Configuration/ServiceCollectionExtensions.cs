using CoffeeNap.Data;
using CoffeeNap.Services;
using CoffeeNap.ViewModels;
using CoffeeNap.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeNap;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddCoffeeNap(this IServiceCollection services)
    {
        services.AddSingleton<AppDatabase>();

        services.AddSingleton<IAppDataService, AppDataService>();
        services.AddSingleton<UserStateService>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IAppNavigationService, AppNavigationService>();
#if ANDROID
        services.AddSingleton<IApplicationLifecycleService, Platforms.Android.AndroidApplicationLifecycleService>();
#else
        services.AddSingleton<IApplicationLifecycleService, DefaultApplicationLifecycleService>();
#endif
        services.AddSingleton<ICaffeineCalculator, CaffeineCalculator>();
        services.AddSingleton<CalendarStatisticsService>();

        services.AddTransient<MainHeaderViewModel>();
        services.AddTransient<BottomNavigationViewModel>();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<CalendarPageViewModel>();
        services.AddSingleton<AddConsumptionPageViewModel>();
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<SettingsPageViewModel>();

        services.AddTransient<MainPage>();
        services.AddTransient<CalendarPage>();
        services.AddSingleton<AddConsumptionPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<OnboardingPage>();
        services.AddTransient<PrivacyPolicyPage>();
        services.AddTransient<TabHostPage>();

        services.AddSingleton<AppPageFactory>();

        return services;
    }
}
