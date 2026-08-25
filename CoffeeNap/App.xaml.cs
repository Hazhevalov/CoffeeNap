using CoffeeNap.Services;
using CoffeeNap.Views;
using Microsoft.Extensions.Logging;

namespace CoffeeNap;

/// <summary>Создаёт окно и завершает startup data flow до показа AppShell.</summary>
public partial class App : Application
{
    private readonly IAppDataService _dataService;
    private readonly AppPageFactory _pageFactory;
    private readonly ILogger<App> _logger;

    public App(
        IAppDataService dataService,
        AppPageFactory pageFactory,
        ILogger<App> logger)
    {
        InitializeComponent();
        _dataService = dataService;
        _pageFactory = pageFactory;
        _logger = logger;
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
            await _dataService.InitializeAsync();
            var profile = await _dataService.GetUserProfileAsync() ?? new();

            await MainThread.InvokeOnMainThreadAsync(() =>
                window.Page = new AppShell(
                    profile,
                    _pageFactory.CreateOnboardingPage(),
                    _pageFactory.CreateMainPage()));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Application data initialization failed.");
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
