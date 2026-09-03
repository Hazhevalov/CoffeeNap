using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

/// <summary>Логика переиспользуемой шапки, независимая от PageViewModel.</summary>
public partial class MainHeaderViewModel : ObservableObject
{
    private readonly IAppNavigationService _navigationService;

    public MainHeaderViewModel(
        UserStateService userState,
        IAppNavigationService navigationService)
    {
        UserState = userState;
        _navigationService = navigationService;
    }

    public UserStateService UserState { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenSettingsAsync() => _navigationService.OpenSettingsAsync();
}
