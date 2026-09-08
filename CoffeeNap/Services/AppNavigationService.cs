using CoffeeNap.Helpers;
using CoffeeNap.Views;

namespace CoffeeNap.Services;

/// <summary>Serializes root and modal application navigation.</summary>
public sealed class AppNavigationService : IAppNavigationService
{
    private static readonly TimeSpan BackNavigationDebounce = TimeSpan.FromMilliseconds(350);
    private static readonly string[] TopLevelRoutes =
    [
        AppShell.MainAbsoluteRoute,
        AppShell.AddConsumptionAbsoluteRoute,
        AppShell.CalendarAbsoluteRoute
    ];

    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly List<string> _topLevelHistory = [];
    private NavigationPage? _settingsNavigationPage;
    private PrivacyPolicyPage? _privacyPolicyPage;
    private WeakReference<Shell>? _ownerShell;
    private long _lastBackNavigationTimestamp;

    public AppNavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public Task OpenSettingsAsync() => RunNavigationAsync("Section -> Settings", async shell =>
    {
        if (shell.Navigation.ModalStack.Count > 0)
        {
            return;
        }

        var navigationPage = GetOrCreateSettingsNavigationPage(shell);
        await shell.Navigation.PushModalAsync(navigationPage, true);
    });

    public Task OpenPrivacyPolicyAsync() => RunNavigationAsync("Settings -> Privacy", async shell =>
    {
        if (shell.Navigation.ModalStack.LastOrDefault() is not NavigationPage navigationPage ||
            navigationPage.CurrentPage is PrivacyPolicyPage)
        {
            return;
        }

        var privacyPage = _privacyPolicyPage ??=
            _services.GetRequiredService<PrivacyPolicyPage>();
        await navigationPage.PushAsync(privacyPage, true);
    });

    public Task GoBackAsync() => RunNavigationAsync("Modal back", async shell =>
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
            await navigationPage.PopAsync(true);
            return;
        }

        await shell.Navigation.PopModalAsync(true);
    });

    public async Task NavigateToTopLevelAsync(string absoluteRoute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteRoute);

        if (!TopLevelRoutes.Contains(absoluteRoute, StringComparer.Ordinal))
        {
            throw new ArgumentException("Unknown top-level route.", nameof(absoluteRoute));
        }

        await RunNavigationAsync($"Top-level -> {absoluteRoute}", async shell =>
        {
            var currentRoute = GetCurrentTopLevelRoute(shell);
            var nextHistory = BuildHistoryForForwardNavigation(
                currentRoute,
                absoluteRoute);

            if (!string.Equals(currentRoute, absoluteRoute, StringComparison.Ordinal))
            {
                // Когда Settings открыта модально, сначала меняем скрытый root,
                // затем одним Pop убираем modal. Поэтому старая Settings не
                // успевает появиться между выбранным разделом и конечным кадром.
                await shell.GoToAsync(
                    absoluteRoute,
                    animate: shell.Navigation.ModalStack.Count == 0);
            }

            if (shell.Navigation.ModalStack.Count > 0)
            {
                await shell.Navigation.PopModalAsync(true);
            }

            _topLevelHistory.Clear();
            _topLevelHistory.AddRange(nextHistory);
        });
    }

    public Task NavigateBackFromTopLevelAsync() =>
        RunNavigationAsync("Top-level back", async shell =>
        {
            if (IsBackNavigationDebounced())
            {
                return;
            }

            var currentRoute = GetCurrentTopLevelRoute(shell);
            if (string.Equals(currentRoute, AppShell.MainAbsoluteRoute, StringComparison.Ordinal))
            {
                return;
            }

            var targetRoute = _topLevelHistory.Count > 0
                ? _topLevelHistory[^1]
                : AppShell.MainAbsoluteRoute;

            await shell.GoToAsync(targetRoute, true);

            if (_topLevelHistory.Count > 0)
            {
                _topLevelHistory.RemoveAt(_topLevelHistory.Count - 1);
            }
        });

    private List<string> BuildHistoryForForwardNavigation(
        string? currentRoute,
        string targetRoute)
    {
        if (string.Equals(targetRoute, AppShell.MainAbsoluteRoute, StringComparison.Ordinal))
        {
            return [];
        }

        var history = new List<string>(_topLevelHistory);
        var existingTargetIndex = history.FindLastIndex(route =>
            string.Equals(route, targetRoute, StringComparison.Ordinal));
        if (existingTargetIndex >= 0)
        {
            history.RemoveRange(existingTargetIndex, history.Count - existingTargetIndex);
            return history;
        }

        if (currentRoute is not null &&
            !string.Equals(currentRoute, targetRoute, StringComparison.Ordinal) &&
            (history.Count == 0 ||
             !string.Equals(history[^1], currentRoute, StringComparison.Ordinal)))
        {
            history.Add(currentRoute);
        }

        // В приложении три root-раздела; лимит дополнительно защищает от
        // накопления истории при будущих изменениях navbar.
        if (history.Count > TopLevelRoutes.Length)
        {
            history.RemoveRange(0, history.Count - TopLevelRoutes.Length);
        }

        return history;
    }

    private static string? GetCurrentTopLevelRoute(Shell shell)
    {
        var shellContentRoute = shell.CurrentItem?.CurrentItem?.CurrentItem?.Route;
        return TopLevelRoutes.FirstOrDefault(route =>
            route.EndsWith($"/{shellContentRoute}", StringComparison.Ordinal));
    }

    private bool IsBackNavigationDebounced()
    {
        var currentTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_lastBackNavigationTimestamp != 0 &&
            System.Diagnostics.Stopwatch.GetElapsedTime(
                _lastBackNavigationTimestamp,
                currentTimestamp) < BackNavigationDebounce)
        {
            return true;
        }

        _lastBackNavigationTimestamp = currentTimestamp;
        return false;
    }

    private NavigationPage GetOrCreateSettingsNavigationPage(Shell shell)
    {
        EnsureWindowContext(shell);
        if (_settingsNavigationPage is not null)
        {
            return _settingsNavigationPage;
        }

        var settingsPage = _services.GetRequiredService<SettingsPage>();
        NavigationPage.SetHasNavigationBar(settingsPage, false);
        _settingsNavigationPage = new NavigationPage(settingsPage);
        return _settingsNavigationPage;
    }

    private void EnsureWindowContext(Shell shell)
    {
        if (_ownerShell?.TryGetTarget(out var owner) == true &&
            ReferenceEquals(owner, shell))
        {
            return;
        }

        _ownerShell = new WeakReference<Shell>(shell);
        _settingsNavigationPage = null;
        _privacyPolicyPage = null;
        _topLevelHistory.Clear();
        _lastBackNavigationTimestamp = 0;
    }

    private async Task RunNavigationAsync(string operation, Func<Shell, Task> navigation)
    {
        var startedAt = PerformanceTrace.Start();
        await _navigationLock.WaitAsync();
        try
        {
            if (Shell.Current is { } shell)
            {
                EnsureWindowContext(shell);
                await navigation(shell);
            }
        }
        finally
        {
            _navigationLock.Release();
            PerformanceTrace.Elapsed(operation, startedAt);
        }
    }
}
