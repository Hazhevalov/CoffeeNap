namespace CoffeeNap.Views;

using CoffeeNap.Helpers;
using CoffeeNap.Services;
using CoffeeNap.ViewModels;

/// <summary>Forwards page lifecycle events to the page-scoped view model.</summary>
public partial class CalendarPage : ContentPage
{
    private readonly CalendarPageViewModel _viewModel;
    private readonly IAppNavigationService _navigationService;
    private bool _isPageVisible;

    public CalendarPage(
        CalendarPageViewModel viewModel,
        IAppNavigationService navigationService)
    {
        var startedAt = PerformanceTrace.Start();
        InitializeComponent();
        PerformanceTrace.Elapsed("CalendarPage.InitializeComponent", startedAt);
        PerformanceTrace.TrackFirstLayout(this, nameof(CalendarPage), startedAt);
        _viewModel = viewModel;
        _navigationService = navigationService;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        var startedAt = PerformanceTrace.Start();
        base.OnAppearing();
        _isPageVisible = true;
        _viewModel.Navigation.ActiveTab = NavigationTab.Calendar;
        await _viewModel.RefreshAsync();
        if (_isPageVisible)
        {
            _viewModel.StartDateChangeMonitor();
        }
        PerformanceTrace.Elapsed("CalendarPage.OnAppearing", startedAt);
    }

    protected override void OnDisappearing()
    {
        _isPageVisible = false;
        _viewModel.StopDateChangeMonitor();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = _navigationService.NavigateBackFromTopLevelAsync();
        return true;
    }
}
