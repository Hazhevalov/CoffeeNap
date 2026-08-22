namespace CoffeeNap
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(AddConsumptionPage), typeof(AddConsumptionPage));
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
        }
    }
}
