using CoffeeNap.Services;
using CoffeeNap.Views;
using Microsoft.Extensions.Logging;

namespace CoffeeNap;

/// <summary>Creates the window and completes startup data loading before showing AppShell.</summary>
public partial class App : Application
{
    private readonly UserStateService _userState;
    private readonly LocalizationService _localization;
    private readonly AppPageFactory _pageFactory;
    private readonly ILogger<App> _logger;
    private readonly CalendarStatisticsService _calendarStatistics;
    private readonly ApplicationVisibility _visibility;

    // Initializes the app.
    public App(
        UserStateService userState,
        LocalizationService localization,
        AppPageFactory pageFactory,
        ILogger<App> logger,
        CalendarStatisticsService calendarStatistics,
        ApplicationVisibility visibility)
    {
        InitializeComponent();
        _userState = userState;
        _localization = localization;
        _pageFactory = pageFactory;
        _logger = logger;
        _calendarStatistics = calendarStatistics;
        _visibility = visibility;
    }

    // Creates the window and starts application initialization.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(CreateLoadingPage());
        window.Activated += (_, _) => _visibility.SetActive(true);
        window.Stopped += (_, _) => _visibility.SetActive(false);
        window.Resumed += (_, _) => _visibility.SetActive(true);
        window.Destroying += (_, _) => (window.Page as AppShell)?.ReleaseContent();
        _ = CompleteStartupAsync(window);
        return window;
    }

    // Loads startup data and displays the shell or an error page.
    private async Task CompleteStartupAsync(Window window)
    {
        try
        {
            await _localization.InitializeAsync();
            await _userState.InitializeAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var shell = new AppShell(_userState, _pageFactory);
                shell.Loaded += OnShellLoaded;
                window.Page = shell;
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Application data initialization failed.");
            await MainThread.InvokeOnMainThreadAsync(() =>
                window.Page = CreateStartupErrorPage());
        }
    }

    // Starts warming the calendar cache after the shell loads.
    private void OnShellLoaded(object? sender, EventArgs eventArgs)
    {
        if (sender is AppShell shell)
        {
            shell.Loaded -= OnShellLoaded;
        }

        // Warm only the data cache. Never construct or bind hidden views on the UI thread.
        _ = Task.Run(WarmUpCalendarDataAsync);
    }

    // Preloads calendar statistics in the background.
    private async Task WarmUpCalendarDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            await _calendarStatistics.GetInitialAsync(today, today).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Calendar data warm-up failed; the page will retry on appearance.");
        }
    }

    // Creates the startup loading screen.
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

    // Creates the localized startup error screen.
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
