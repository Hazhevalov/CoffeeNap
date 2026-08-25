using CoffeeNap.Data;
using CoffeeNap.Services;
using CoffeeNap.ViewModels;
using CoffeeNap.Views;
using Microsoft.Extensions.Logging;

namespace CoffeeNap;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("InterVariableFont.ttf", "Inter");
                fonts.AddFont("InterVariableFont.ttf", "OpenSansRegular");
                fonts.AddFont("InterVariableFont.ttf", "OpenSansSemibold");
            });

#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            nameof(Controls.BorderlessEntry),
            static (handler, view) =>
            {
                if (view is Controls.BorderlessEntry)
                {
                    handler.PlatformView.Background = null;
                }
            });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<AppDatabase>();

        builder.Services.AddSingleton<IAppDataService, AppDataService>();
        builder.Services.AddSingleton<UserStateService>();

        builder.Services.AddTransient<MainHeaderViewModel>();
        builder.Services.AddTransient<BottomNavigationViewModel>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<CalendarPageViewModel>();
        builder.Services.AddTransient<AddConsumptionPageViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<AddConsumptionPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<OnboardingPage>();

        builder.Services.AddSingleton<AppPageFactory>();

        return builder.Build();
    }
}
