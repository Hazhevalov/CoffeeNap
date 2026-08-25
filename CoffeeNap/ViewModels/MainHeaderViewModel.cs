using CoffeeNap.Services;
using CoffeeNap.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

/// <summary>Логика переиспользуемой шапки, независимая от PageViewModel.</summary>
public partial class MainHeaderViewModel : ObservableObject
{
    public MainHeaderViewModel(UserStateService userState)
    {
        UserState = userState;
    }

    public UserStateService UserState { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(nameof(SettingsPage));
}
