namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>
/// Форма добавления напитка; шаги создаются при первом показе.
/// </summary>
public partial class AddConsumptionPage : ContentView, ITabContent
{
    /// <summary>Загружает XAML-разметку страницы.</summary>
    public AddConsumptionPage(AddConsumptionPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    NavigationTab ITabContent.Tab => NavigationTab.AddConsumption;

    Task ITabContent.ActivateAsync() => Task.CompletedTask;

    void ITabContent.Deactivate()
    {
    }

    void ITabContent.Release() => (BindingContext as AddConsumptionPageViewModel)?.Release();
}
