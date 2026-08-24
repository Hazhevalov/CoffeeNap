using System.Windows.Input;

namespace CoffeeNap.Controls;

/// <summary>
/// Переиспользуемая шапка с именем пользователя и внешней командой настроек.
/// Источник данных и навигационная логика остаются у родительской ViewModel.
/// </summary>
public partial class MainHeaderView : ContentView
{
    public static readonly BindableProperty UserNameProperty = BindableProperty.Create(
        nameof(UserName),
        typeof(string),
        typeof(MainHeaderView),
        string.Empty);

    public static readonly BindableProperty SettingsCommandProperty = BindableProperty.Create(
        nameof(SettingsCommand),
        typeof(ICommand),
        typeof(MainHeaderView));

    public MainHeaderView()
    {
        InitializeComponent();
    }

    public string UserName
    {
        get => (string)GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    public ICommand? SettingsCommand
    {
        get => (ICommand?)GetValue(SettingsCommandProperty);
        set => SetValue(SettingsCommandProperty, value);
    }
}
