using CoffeeNap.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoffeeNap;

/// <summary>Создаёт окно и завершает startup data flow до показа AppShell.</summary>
public partial class App : Application
{
    private readonly IAppDataService dataService;
    private readonly AppStartupState startupState;
    private readonly IServiceProvider services;
    private readonly ILogger<App> logger;

    public App(
        IAppDataService dataService,
        AppStartupState startupState,
        IServiceProvider services,
        ILogger<App> logger)
    {
        InitializeComponent();
        this.dataService = dataService;
        this.startupState = startupState;
        this.services = services;
        this.logger = logger;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(CreateLoadingPage());
        _ = CompleteStartupAsync(window);
        return window;
    }

    private async Task CompleteStartupAsync(Window window)
    {
        try
        {
            await dataService.InitializeAsync();
            startupState.UserProfile = await dataService.GetUserProfileAsync() ?? new();

            await MainThread.InvokeOnMainThreadAsync(() =>
                window.Page = services.GetRequiredService<AppShell>());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Application data initialization failed.");
            await MainThread.InvokeOnMainThreadAsync(() =>
                window.Page = CreateStartupErrorPage());
        }
    }

    private static ContentPage CreateLoadingPage() => new()
    {
        BackgroundColor = Color.FromArgb("#F7F7F7"),
        Content = new ActivityIndicator
        {
            IsRunning = true,
            Color = Colors.Black,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        }
    };

    private static ContentPage CreateStartupErrorPage() => new()
    {
        BackgroundColor = Color.FromArgb("#F7F7F7"),
        Content = new Label
        {
            Text = "Не удалось загрузить данные приложения. Перезапустите CoffeeNap.",
            TextColor = Colors.Black,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(32)
        }
    };
}
