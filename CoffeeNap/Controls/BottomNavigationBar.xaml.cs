using System.Windows.Input;

namespace CoffeeNap.Controls;

public partial class BottomNavigationBar : ContentView
{
    // BindableProperty позволяет передавать команды из ViewModel родительской
    // страницы, не связывая переиспользуемую панель с конкретной навигацией.
    public static readonly BindableProperty AddCommandProperty = BindableProperty.Create(
        nameof(AddCommand),
        typeof(ICommand),
        typeof(BottomNavigationBar));

    public static readonly BindableProperty CalendarCommandProperty = BindableProperty.Create(
        nameof(CalendarCommand),
        typeof(ICommand),
        typeof(BottomNavigationBar));

    public BottomNavigationBar()
    {
        InitializeComponent();
    }

    public ICommand? AddCommand
    {
        get => (ICommand?)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    public ICommand? CalendarCommand
    {
        get => (ICommand?)GetValue(CalendarCommandProperty);
        set => SetValue(CalendarCommandProperty, value);
    }
}
