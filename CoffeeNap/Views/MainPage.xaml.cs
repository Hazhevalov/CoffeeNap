using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>
/// Главный экран приложения. Разметка находится в MainPage.xaml, а code-behind
/// отвечает только за жизненный цикл MainPageViewModel.
/// </summary>
public partial class MainPage : ContentView, ITabContent
{
    private readonly MainPageViewModel _viewModel;
    private bool _isPageVisible;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    NavigationTab ITabContent.Tab => NavigationTab.Home;

    async Task ITabContent.ActivateAsync()
    {
        _isPageVisible = true;
        await _viewModel.InitializeAsync();
        if (_isPageVisible)
        {
            ConsumptionPanel.ScrollToNewest();
            _viewModel.StartRelativeTimeTimer();
        }
    }

    void ITabContent.Deactivate()
    {
        _isPageVisible = false;
        _viewModel.StopRelativeTimeTimer();
    }
}
