namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>
/// Заготовка формы добавления употребления. Поля формы пока не реализованы.
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
}
