using CoffeeNap.Services;
using CoffeeNap.Views;
using Microsoft.Extensions.Logging;

namespace CoffeeNap;

/// <summary>Создаёт окно и завершает startup data flow до показа AppShell.</summary>
public partial class App : Application
{
    private readonly UserStateService _userState;
    private readonly LocalizationService _localization;
    private readonly AppPageFactory _pageFactory;
    private readonly ILogger<App> _logger;

    public App(
        UserStateService userState,
        LocalizationService localization,
        AppPageFactory pageFactory,
        ILogger<App> logger)
    {
        InitializeComponent();
        _userState = userState;
        _localization = localization;
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
            await _localization.InitializeAsync();
            await _userState.InitializeAsync();

            var calendarPage = _pageFactory.CreateCalendarPage();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                window.Page = new AppShell(
                    _userState,
                    _pageFactory.CreateOnboardingPage(),
                    _pageFactory.CreateMainPage(),
                    _pageFactory.CreateAddConsumptionPage(),
                    calendarPage);
            });
            _ = WarmUpCalendarAsync(calendarPage);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Application data initialization failed.");
            await MainThread.InvokeOnMainThreadAsync(() =>
                window.Page = CreateStartupErrorPage());
        }
    }

    private async Task WarmUpCalendarAsync(CalendarPage calendarPage)
    {
        try
        {
            // Let the initially selected page render before doing hidden-page work.
            await Task.Delay(500);
            await calendarPage.WarmUpAsync();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Calendar background warm-up failed.");
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

    private ContentPage CreateStartupErrorPage() => new()
    {
        BackgroundColor = Color.FromArgb("#F7F7F7"),
        Content = new Label
        {
            Text = _localization["StartupFailed"],
            TextColor = Colors.Black,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(32)
        }
    };
}
