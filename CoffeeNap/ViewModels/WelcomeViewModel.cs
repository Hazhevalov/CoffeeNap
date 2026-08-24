using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

/// <summary>Навигация с первого шага onboarding ко вводу имени.</summary>
public partial class WelcomeViewModel
{
    [RelayCommand]
    private static Task StartAsync() =>
        Shell.Current.GoToAsync(nameof(NameSetupPage), false);
}
