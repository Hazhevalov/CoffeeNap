namespace CoffeeNap.Views;

using CoffeeNap.Helpers;
using CoffeeNap.ViewModels;

/// <summary>
/// Заготовка формы добавления употребления. Поля формы пока не реализованы.
/// </summary>
public partial class AddConsumptionPage : ContentPage
{
    private readonly AddConsumptionPageViewModel _viewModel;

    /// <summary>Загружает XAML-разметку страницы.</summary>
    public AddConsumptionPage(AddConsumptionPageViewModel viewModel)
    {
        var startedAt = PerformanceTrace.Start();
        InitializeComponent();
        PerformanceTrace.Elapsed("AddConsumptionPage.InitializeComponent", startedAt);
        PerformanceTrace.TrackFirstLayout(this, nameof(AddConsumptionPage), startedAt);
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = _viewModel.HandleBackAsync();
        return true;
    }

    protected override void OnAppearing()
    {
        var startedAt = PerformanceTrace.Start();
        base.OnAppearing();
        PerformanceTrace.Elapsed("AddConsumptionPage.OnAppearing", startedAt);
    }
}
