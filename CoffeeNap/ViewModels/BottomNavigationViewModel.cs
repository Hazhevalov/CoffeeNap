using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoffeeNap.Services;

namespace CoffeeNap.ViewModels;

/// <summary>
/// Navigation state for one visible bottom bar. The tab host owns its shared
/// instance; modal pages receive an independent instance.
/// </summary>
public partial class BottomNavigationViewModel : ObservableObject
{
    private readonly IAppNavigationService _navigationService;
    private NavigationTab _activeTab;
    private bool _animateNextSelection = true;

    public BottomNavigationViewModel(IAppNavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public NavigationTab ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                OnPropertyChanged(nameof(IsHomeActive));
                OnPropertyChanged(nameof(IsAddConsumptionActive));
                OnPropertyChanged(nameof(IsCalendarActive));
            }
        }
    }

    public bool IsHomeActive => ActiveTab == NavigationTab.Home;

    public bool IsAddConsumptionActive => ActiveTab == NavigationTab.AddConsumption;

    public bool IsCalendarActive => ActiveTab == NavigationTab.Calendar;

    internal void SetActiveTab(NavigationTab tab, bool animate)
    {
        _animateNextSelection = animate;
        try
        {
            ActiveTab = tab;
        }
        finally
        {
            _animateNextSelection = true;
        }
    }

    internal bool ConsumeSelectionAnimation()
    {
        var animate = _animateNextSelection;
        _animateNextSelection = true;
        return animate;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenHomeAsync() => NavigateAsync(
        NavigationTab.Home,
        AppShell.MainAbsoluteRoute);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenAddConsumptionAsync() => NavigateAsync(
        NavigationTab.AddConsumption,
        AppShell.AddConsumptionAbsoluteRoute);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenCalendarAsync() => NavigateAsync(
        NavigationTab.Calendar,
        AppShell.CalendarAbsoluteRoute);

    private Task NavigateAsync(NavigationTab targetTab, string absoluteRoute)
    {
        if (ActiveTab == targetTab)
        {
            return Task.CompletedTask;
        }

        // The host changes ActiveTab only when the destination is ready. No
        // optimistic rollback is needed, and an already committed destination
        // can never diverge from the pill if native navigation later fails.
        return _navigationService.NavigateToTopLevelAsync(absoluteRoute, this);
    }
}
