using CoffeeNap.Services;
using CoffeeNap.Views;

namespace CoffeeNap;

/// <summary>Creates the shell after loading the startup profile from SQLite.</summary>
public partial class AppShell : Shell
{
    public const string MainAbsoluteRoute = "//AppTabs/MainPage";
    public const string AddConsumptionAbsoluteRoute = "//AppTabs/AddConsumptionPage";
    public const string CalendarAbsoluteRoute = "//AppTabs/CalendarPage";

    private readonly AppPageFactory _pageFactory;
    private TabHostPage? _tabHostPage;

    // Initializes the app shell.
    public AppShell(
        UserStateService userState,
        AppPageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        InitializeComponent();
        // Shell materializes only the selected root. The host then creates each
        // tab lazily and keeps it alive so form and calendar state are preserved.
        OnboardingShellContent.ContentTemplate = new DataTemplate(() => pageFactory.CreateOnboardingPage());
        TabHostShellContent.ContentTemplate = new DataTemplate(() => GetTabHostPage());

        CurrentItem = userState.IsOnboardingCompleted && userState.HasUserName
            ? MainShellItem
            : OnboardingShellItem;
    }

    // Returns the shell's tab host page.
    internal TabHostPage GetTabHostPage() =>
        _tabHostPage ??= _pageFactory.CreateTabHostPage();

    // Releases the content owned by the shell.
    internal void ReleaseContent()
    {
        _tabHostPage?.Release();
        _tabHostPage = null;
    }
}
