using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>
/// Main application screen with layout defined in MainPage.xaml.
/// The code-behind manages the MainPageViewModel lifecycle.
/// </summary>
public partial class MainPage : ContentView, ITabContent
{
    private readonly MainPageViewModel _viewModel;
    private bool _isPageVisible;

    // Initializes the main page.
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        ConsumptionPanel.VisibleRangeChanged += viewModel.SetVisibleRange;
    }

    NavigationTab ITabContent.Tab => NavigationTab.Home;

    // Loads main page data and starts relative-time updates.
    async Task ITabContent.ActivateAsync()
    {
        _isPageVisible = true;
        await _viewModel.InitializeAsync();
        if (_isPageVisible)
        {
            _viewModel.StartRelativeTimeTimer();
        }
    }

    // Stops relative-time updates while the main tab is hidden.
    void ITabContent.Deactivate()
    {
        _isPageVisible = false;
        _viewModel.StopRelativeTimeTimer();
    }

    // Releases main page resources and event subscriptions.
    void ITabContent.Release()
    {
        _isPageVisible = false;
        ConsumptionPanel.VisibleRangeChanged -= _viewModel.SetVisibleRange;
        _viewModel.Release();
    }
}
