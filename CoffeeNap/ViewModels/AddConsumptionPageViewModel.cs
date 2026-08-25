using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.ViewModels;

public sealed class AddConsumptionPageViewModel : ObservableObject
{
    public AddConsumptionPageViewModel(
        MainHeaderViewModel header,
        BottomNavigationViewModel navigation)
    {
        Header = header;
        Navigation = navigation;
        Navigation.ActiveTab = NavigationTab.AddConsumption;
    }

    public MainHeaderViewModel Header { get; }

    public BottomNavigationViewModel Navigation { get; }
}
