using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.ViewModels;

public sealed class CalendarPageViewModel : ObservableObject
{
    public CalendarPageViewModel(
        MainHeaderViewModel header,
        BottomNavigationViewModel navigation)
    {
        Header = header;
        Navigation = navigation;
        // Нав бар активной становится иконка календаря
        Navigation.ActiveTab = NavigationTab.Calendar;
    }

    public MainHeaderViewModel Header { get; }

    public BottomNavigationViewModel Navigation { get; }
}
