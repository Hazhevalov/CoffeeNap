using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

using CoffeeNap.Helpers;
using CoffeeNap.Services;

/// <summary>
/// Главный экран приложения. Разметка находится в MainPage.xaml, а code-behind
/// отвечает только за жизненный цикл MainPageViewModel.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;
    private readonly CalendarStatisticsService _calendarStatisticsService;
    private bool _isPageVisible;
    private bool _calendarPrefetchStarted;

    public MainPage(
        MainPageViewModel viewModel,
        CalendarStatisticsService calendarStatisticsService)
    {
        var startedAt = PerformanceTrace.Start();
        InitializeComponent();
        PerformanceTrace.Elapsed("MainPage.InitializeComponent", startedAt);
        PerformanceTrace.TrackFirstLayout(this, nameof(MainPage), startedAt);
        _viewModel = viewModel;
        _calendarStatisticsService = calendarStatisticsService;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        var startedAt = PerformanceTrace.Start();
        base.OnAppearing();
        _isPageVisible = true;
        await _viewModel.InitializeAsync();
        if (_isPageVisible)
        {
            ConsumptionPanel.ScrollToNewest();
            _viewModel.StartRelativeTimeTimer();
            if (!_calendarPrefetchStarted)
            {
                _calendarPrefetchStarted = true;
                _calendarStatisticsService.PrefetchCurrent(DateTime.Today);
            }
        }
        PerformanceTrace.Elapsed("MainPage.OnAppearing", startedAt);
    }

    protected override void OnDisappearing()
    {
        _isPageVisible = false;
        _viewModel.StopRelativeTimeTimer();
        base.OnDisappearing();
    }
}
