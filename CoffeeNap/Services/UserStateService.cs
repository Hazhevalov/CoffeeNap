using CoffeeNap.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.Services;

/// <summary>
/// Единое observable-состояние профиля во время работы приложения. Persistent
/// данные читаются и записываются только через IAppDataService.
/// </summary>
public sealed class UserStateService : ObservableObject
{
    private const string DefaultUserName = "Пользователь";
    private readonly IAppDataService _dataService;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private UserProfile _profile = new();
    private string _userName = DefaultUserName;
    private bool _isOnboardingCompleted;
    private bool _isInitialized;

    public UserStateService(IAppDataService dataService)
    {
        _dataService = dataService;
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

    //public async Task UpdateUserNameAsync(string userName)
    //{
    //    var normalizedName = NormalizeUserName(userName);
    //    await InitializeAsync();

    //    await _operationLock.WaitAsync();
    //    try
    //    {
    //        _profile.UserName = normalizedName;
    //        await _dataService.SaveUserProfileAsync(_profile);
    //        await PublishProfileAsync(_profile);
    //    }
    //    finally
    //    {
    //        _operationLock.Release();
    //    }
    //}

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

    private async Task PublishProfileAsync(UserProfile profile)
    {
        var displayName = string.IsNullOrWhiteSpace(profile.UserName)
            ? DefaultUserName
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
}
