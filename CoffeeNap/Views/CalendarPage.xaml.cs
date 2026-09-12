namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>Forwards page lifecycle events to the page-scoped view model.</summary>
public partial class CalendarPage : ContentView, ITabContent
{
    private readonly CalendarPageViewModel _viewModel;
    private bool _isPageVisible;

    public CalendarPage(CalendarPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    NavigationTab ITabContent.Tab => NavigationTab.Calendar;

    async Task ITabContent.ActivateAsync()
    {
        _isPageVisible = true;
        await _viewModel.RefreshAsync();
        if (_isPageVisible)
        {
            _viewModel.StartDateChangeMonitor();
        }
    }

    void ITabContent.Deactivate()
    {
        _isPageVisible = false;
        _viewModel.StopDateChangeMonitor();
    }

    internal Task WarmUpAsync() => _viewModel.WarmUpAsync();
}
