namespace CoffeeNap
{
    /// <summary>
    /// Навигационная оболочка приложения. AppShell.xaml задаёт стартовую страницу,
    /// а конструктор регистрирует маршруты остальных экранов.
    /// </summary>
    public partial class AppShell : Shell
    {
        /// <summary>Загружает Shell и подготавливает маршруты для GoToAsync.</summary>
        public AppShell()
        {
            InitializeComponent();
            // Регистрируем страницы без фиксированного места в Shell,
            // чтобы открывать их по имени через Shell.Current.GoToAsync.
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(AddConsumptionPage), typeof(AddConsumptionPage));
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));

            var hasName = Services.UserPreferencesService.GetUserName() is not null;
            CurrentItem = Services.UserPreferencesService.IsOnboardingCompleted() && hasName
                ? MainShellItem
                : OnboardingShellItem;
        }
    }
}
