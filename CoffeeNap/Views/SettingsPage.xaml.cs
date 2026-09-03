using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>
/// Экран настроек; вся логика предоставляется SettingsPageViewModel.
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
