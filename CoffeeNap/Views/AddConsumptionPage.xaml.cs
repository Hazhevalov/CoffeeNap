namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>
/// Заготовка формы добавления употребления. Поля формы пока не реализованы.
/// </summary>
public partial class AddConsumptionPage : ContentPage
{
    /// <summary>Загружает XAML-разметку страницы.</summary>
    public AddConsumptionPage(AddConsumptionPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
