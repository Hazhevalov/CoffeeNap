using CoffeeNap.Views;
using CoffeeNap.ViewModels;

namespace CoffeeNap.Services;

/// <summary>Serializes root and modal application navigation.</summary>
public sealed class AppNavigationService : IAppNavigationService
{
    private static readonly TimeSpan BackNavigationDebounce = TimeSpan.FromMilliseconds(350);

    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private long _lastBackNavigationTimestamp;

    public AppNavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public Task OpenSettingsAsync() => RunNavigationAsync(async shell =>
    {
        if (shell.Navigation.ModalStack.Count > 0)
        {
            return;
        }

        var settingsPage = _services.GetRequiredService<SettingsPage>();
        NavigationPage.SetHasNavigationBar(settingsPage, false);
        var navigationPage = new NavigationPage(settingsPage);
        await shell.Navigation.PushModalAsync(navigationPage, NavigationAnimation.IsEnabled);
    });

    public Task OpenPrivacyPolicyAsync() => RunNavigationAsync(async shell =>
    {
        if (shell.Navigation.ModalStack.LastOrDefault() is not NavigationPage navigationPage ||
            navigationPage.CurrentPage is PrivacyPolicyPage)
        {
            return;
        }

        var privacyPage = _services.GetRequiredService<PrivacyPolicyPage>();
        await navigationPage.PushAsync(privacyPage, NavigationAnimation.IsEnabled);
    });

    public Task GoBackAsync() => RunNavigationAsync(async shell =>
    {
        if (shell.Navigation.ModalStack.LastOrDefault() is not NavigationPage navigationPage)
        {
            return;
        }

        var currentTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_lastBackNavigationTimestamp != 0 &&
            System.Diagnostics.Stopwatch.GetElapsedTime(
                _lastBackNavigationTimestamp,
                currentTimestamp) < BackNavigationDebounce)
        {
            return;
        }

        _lastBackNavigationTimestamp = currentTimestamp;

        if (navigationPage.Navigation.NavigationStack.Count > 1)
        {
            await navigationPage.PopAsync(NavigationAnimation.IsEnabled);
            return;
        }

        await shell.Navigation.PopModalAsync(NavigationAnimation.IsEnabled);
    });

    public async Task NavigateToTopLevelAsync(
        string absoluteRoute,
        BottomNavigationViewModel? sourceNavigation = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteRoute);
        var targetTab = GetTab(absoluteRoute);
        if (targetTab == NavigationTab.None)
        {
            throw new ArgumentException("Unknown top-level route.", nameof(absoluteRoute));
        }

        await RunNavigationAsync(async shell =>
        {
            if (shell is not AppShell appShell)
            {
                return;
            }

            var modalIsOpen = shell.Navigation.ModalStack.Count > 0;
            var host = shell.CurrentPage as TabHostPage ?? appShell.GetTabHostPage();
            var previousSourceTab = sourceNavigation?.ActiveTab ?? NavigationTab.None;

            // A modal covers the host, so select its target without motion and
            // let the native dismiss animation reveal the correct section.
            await host.NavigateToAsync(targetTab, sourceNavigation);

            if (!modalIsOpen && !ReferenceEquals(shell.CurrentPage, host))
            {
                await shell.GoToAsync(
                    AppShell.MainAbsoluteRoute,
                    NavigationAnimation.IsEnabled);
            }

            if (modalIsOpen)
            {
                try
                {
                    await shell.Navigation.PopModalAsync(NavigationAnimation.IsEnabled);
                }
                catch
                {
                    // If the native modal dismiss fails, its bottom bar remains
                    // visible. Restore only that bar; the host was committed and
                    // is already ready behind the modal for a safe retry.
                    if (sourceNavigation is not null &&
                        !ReferenceEquals(sourceNavigation, host.BottomNavigation))
                    {
                        sourceNavigation.SetActiveTab(previousSourceTab, animate: false);
                    }
                    throw;
                }
            }
        }, dropIfBusy: sourceNavigation is not null);
    }

    private static NavigationTab GetTab(string absoluteRoute) => absoluteRoute switch
    {
        AppShell.MainAbsoluteRoute => NavigationTab.Home,
        AppShell.AddConsumptionAbsoluteRoute => NavigationTab.AddConsumption,
        AppShell.CalendarAbsoluteRoute => NavigationTab.Calendar,
        _ => NavigationTab.None
    };

    private async Task RunNavigationAsync(
        Func<Shell, Task> navigation,
        bool dropIfBusy = false)
    {
        var lockAcquired = dropIfBusy
            ? await _navigationLock.WaitAsync(0)
            : await WaitForNavigationLockAsync();

        if (!lockAcquired)
        {
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is { } shell)
                {
                    await navigation(shell);
                }
            });
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    private async Task<bool> WaitForNavigationLockAsync()
    {
        await _navigationLock.WaitAsync();
        return true;
    }
}
