using System.Windows.Input;

namespace CoffeeNap.Controls;

/// <summary>
/// Переиспользуемая нижняя навигационная панель. Она не знает о Shell и получает
/// действия средней и правой кнопок как команды от родительской ViewModel.
/// </summary>
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

    /// <summary>Создаёт контрол и загружает разметку BottomNavigationBar.xaml.</summary>
    public BottomNavigationBar()
    {
        InitializeComponent();
    }

    /// <summary>Команда кнопки «добавить употребление».</summary>
    public ICommand? AddCommand
    {
        get => (ICommand?)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    /// <summary>Команда перехода к календарю.</summary>
    public ICommand? CalendarCommand
    {
        get => (ICommand?)GetValue(CalendarCommandProperty);
        set => SetValue(CalendarCommandProperty, value);
    }
}
