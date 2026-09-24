using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

/// <summary>Reusable header logic independent of page view models.</summary>
public partial class MainHeaderViewModel : ObservableObject
{
    private readonly IAppNavigationService _navigationService;

    // Initializes the main header view model.
    public MainHeaderViewModel(
        UserStateService userState,
        IAppNavigationService navigationService)
    {
        UserState = userState;
        _navigationService = navigationService;
    }

    public UserStateService UserState { get; }

    // Opens the settings page.
    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenSettingsAsync() => _navigationService.OpenSettingsAsync();
}
