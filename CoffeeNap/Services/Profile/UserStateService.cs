using CoffeeNap.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.Services;

/// <summary>
/// Shared observable profile state for the running application.
/// Reads and writes persistent data only through IAppDataService.
/// </summary>
public sealed class UserStateService : ObservableObject
{
    private readonly IAppDataService _dataService;
    private readonly LocalizationService _localization;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private UserProfile _profile = new();
    private string _userName;
    private bool _isOnboardingCompleted;
    private bool _isInitialized;

    // Initializes the user state service.
    public UserStateService(
        IAppDataService dataService,
        LocalizationService localization)
    {
        _dataService = dataService;
        _localization = localization;
        _userName = localization["DefaultUserName"];
        _dataService.UserDataDeleted += OnUserDataDeleted;
        _localization.CultureChanged += OnCultureChanged;
    }

    public string UserName
    {
        get => _userName;
        private set => SetProperty(ref _userName, value);
    }

    public bool IsOnboardingCompleted
    {
        get => _isOnboardingCompleted;
        private set => SetProperty(ref _isOnboardingCompleted, value);
    }

    public bool HasUserName => !string.IsNullOrWhiteSpace(_profile.UserName);

    public bool IsInitialized => _isInitialized;

    // Loads the user profile into shared observable state.
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _operationLock.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await _dataService.InitializeAsync();
            _profile = await _dataService.GetUserProfileAsync() ?? new UserProfile();
            await PublishProfileAsync(_profile);
            _isInitialized = true;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    // Saves the user name and completes onboarding.
    public async Task CompleteOnboardingAsync(string userName)
    {
        var normalizedName = NormalizeUserName(userName);
        await InitializeAsync();

        await _operationLock.WaitAsync();
        try
        {
            _profile.UserName = normalizedName;
            _profile.OnboardingCompleted = true;
            await _dataService.SaveUserProfileAsync(_profile);
            await PublishProfileAsync(_profile);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    // Publishes profile changes on the main thread.
    private async Task PublishProfileAsync(UserProfile profile)
    {
        var displayName = string.IsNullOrWhiteSpace(profile.UserName)
            ? _localization["DefaultUserName"]
            : profile.UserName;

        if (MainThread.IsMainThread)
        {
            UserName = displayName;
            IsOnboardingCompleted = profile.OnboardingCompleted;
            OnPropertyChanged(nameof(HasUserName));
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            UserName = displayName;
            IsOnboardingCompleted = profile.OnboardingCompleted;
            OnPropertyChanged(nameof(HasUserName));
        });
    }

    // Trims the user name and validates its length.
    private static string NormalizeUserName(string userName)
    {
        ArgumentNullException.ThrowIfNull(userName);
        var normalizedName = userName.Trim();
        if (normalizedName.Length is 0 or > UserProfile.MaximumUserNameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userName),
                $"User name must contain from 1 to {UserProfile.MaximumUserNameLength} characters.");
        }

        return normalizedName;
    }

    // Resets the shared profile after user data is deleted.
    private void OnUserDataDeleted(object? sender, EventArgs eventArgs)
    {
        _profile = new UserProfile();
        _isInitialized = false;
        UserName = _localization["DefaultUserName"];
        IsOnboardingCompleted = false;
        OnPropertyChanged(nameof(HasUserName));
        OnPropertyChanged(nameof(IsInitialized));
    }

    // Refreshes profile display values after a culture change.
    private void OnCultureChanged(object? sender, EventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(_profile.UserName))
        {
            UserName = _localization["DefaultUserName"];
        }
    }
}
