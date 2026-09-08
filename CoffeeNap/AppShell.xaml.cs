using CoffeeNap.Services;
using CoffeeNap.Views;

namespace CoffeeNap;

/// <summary>Shell создаётся только после чтения startup profile из SQLite.</summary>
public partial class AppShell : Shell
{
    public const string MainAbsoluteRoute = "//AppTabs/MainPage";
    public const string AddConsumptionAbsoluteRoute = "//AppTabs/AddConsumptionPage";
    public const string CalendarAbsoluteRoute = "//AppTabs/CalendarPage";

    public AppShell(
        UserStateService userState,
        AppPageFactory pageFactory)
    {
        InitializeComponent();
        // Shell создаёт только выбранный root. Остальные разделы создаются при
        // первом переходе и затем удерживаются самим Shell без дубликатов.
        OnboardingShellContent.ContentTemplate =
            new DataTemplate(pageFactory.CreateOnboardingPage);
        MainShellContent.ContentTemplate =
            new DataTemplate(pageFactory.CreateMainPage);
        AddConsumptionShellContent.ContentTemplate =
            new DataTemplate(pageFactory.CreateAddConsumptionPage);
        CalendarShellContent.ContentTemplate =
            new DataTemplate(pageFactory.CreateCalendarPage);

        CurrentItem = userState.IsOnboardingCompleted && userState.HasUserName
            ? MainShellItem
            : OnboardingShellItem;
    }
}
