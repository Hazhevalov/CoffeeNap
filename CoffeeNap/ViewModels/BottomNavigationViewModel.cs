using CoffeeNap.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

/// <summary>
/// Общая навигационная логика нижней панели. Экземпляр принадлежит конкретной
/// странице, поэтому active state не конфликтует с другими экранами.
/// </summary>
public partial class BottomNavigationViewModel : ObservableObject
{
    private NavigationTab _activeTab;

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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenHomeAsync() => ActiveTab == NavigationTab.Home
        ? Task.CompletedTask
        : Shell.Current.GoToAsync(AppShell.MainAbsoluteRoute, true);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenAddConsumptionAsync() => ActiveTab == NavigationTab.AddConsumption
        ? Task.CompletedTask
        : Shell.Current.GoToAsync(nameof(AddConsumptionPage));

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenCalendarAsync() => ActiveTab == NavigationTab.Calendar
        ? Task.CompletedTask
        : Shell.Current.GoToAsync(nameof(CalendarPage));
}
