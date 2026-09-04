namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>Forwards page lifecycle events to the page-scoped view model.</summary>
public partial class CalendarPage : ContentPage
{
    private readonly CalendarPageViewModel _viewModel;
    private bool _isPageVisible;

    public CalendarPage(CalendarPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageVisible = true;
        _viewModel.Navigation.ActiveTab = NavigationTab.Calendar;
        await _viewModel.RefreshAsync();
        if (_isPageVisible)
        {
            _viewModel.StartDateChangeMonitor();
        }
    }

    protected override void OnDisappearing()
    {
        _isPageVisible = false;
        _viewModel.StopDateChangeMonitor();
        base.OnDisappearing();
    }

    internal Task WarmUpAsync() => _viewModel.WarmUpAsync();
}
