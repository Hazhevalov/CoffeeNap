namespace CoffeeNap.Views;

using CoffeeNap.ViewModels;

/// <summary>
/// Заготовка экрана календаря истории. Сейчас содержит только статический placeholder.
/// </summary>
public partial class CalendarPage : ContentPage
{
    /// <summary>Загружает XAML-разметку страницы.</summary>
    public CalendarPage(CalendarPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
