using CoffeeNap.Views;

namespace CoffeeNap.Services;

/// <summary>Serializes root and modal application navigation.</summary>
public sealed class AppNavigationService : IAppNavigationService
{
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);

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
        await shell.Navigation.PushModalAsync(navigationPage, false);
    });

    public Task OpenPrivacyPolicyAsync() => RunNavigationAsync(async shell =>
    {
        if (shell.Navigation.ModalStack.LastOrDefault() is not NavigationPage navigationPage ||
            navigationPage.CurrentPage is PrivacyPolicyPage)
        {
            return;
        }

        var privacyPage = _services.GetRequiredService<PrivacyPolicyPage>();
        await navigationPage.PushAsync(privacyPage, true);
    });

    public Task GoBackAsync() => RunNavigationAsync(async shell =>
    {
        if (shell.Navigation.ModalStack.LastOrDefault() is not NavigationPage navigationPage)
        {
            return;
        }

        if (navigationPage.Navigation.NavigationStack.Count > 1)
        {
            await navigationPage.PopAsync(false);
            return;
        }

        await shell.Navigation.PopModalAsync(false);
    });

    public async Task NavigateToTopLevelAsync(string absoluteRoute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteRoute);

        await RunNavigationAsync(async shell =>
        {
            if (shell.Navigation.ModalStack.Count > 0)
            {
                await shell.Navigation.PopModalAsync(false);
            }

            await shell.GoToAsync(absoluteRoute, false);
        });
    }

    private async Task RunNavigationAsync(Func<Shell, Task> navigation)
    {
        await _navigationLock.WaitAsync();
        try
        {
            if (Shell.Current is { } shell)
            {
                await navigation(shell);
            }
        }
        finally
        {
            _navigationLock.Release();
        }
    }
}
