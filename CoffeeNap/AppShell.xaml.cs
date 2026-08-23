namespace CoffeeNap
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Регистрируем страницы без фиксированного места в Shell,
            // чтобы открывать их по имени через Shell.Current.GoToAsync.
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(AddConsumptionPage), typeof(AddConsumptionPage));
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
        }
    }
}
