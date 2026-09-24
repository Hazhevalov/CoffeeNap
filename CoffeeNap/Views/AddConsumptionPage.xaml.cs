namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>
/// Drink entry form that creates each step when first shown.
/// </summary>
public partial class AddConsumptionPage : ContentView, ITabContent
{
    /// <summary>Loads the page's XAML layout.</summary>
    public AddConsumptionPage(AddConsumptionPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    NavigationTab ITabContent.Tab => NavigationTab.AddConsumption;

    // Completes activation without additional loading.
    Task ITabContent.ActivateAsync() => Task.CompletedTask;

    // Leaves the quiz state intact when the tab is hidden.
    void ITabContent.Deactivate()
    {
    }

    // Releases the quiz view model's event subscriptions.
    void ITabContent.Release() => (BindingContext as AddConsumptionPageViewModel)?.Release();
}
