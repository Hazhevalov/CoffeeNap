using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly LocalizationService _localization;
    private readonly IAppDataService _dataService;
    private readonly IApplicationLifecycleService _lifecycleService;
    private readonly IAppNavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsPageViewModel> _logger;
    private bool _isLanguagePanelVisible;
    private bool _isDeleteConfirmationVisible;
    private bool _isDeletingData;

    public event Func<Task>? LanguageMenuTransitionRequested;
    public event Func<Task>? DeleteWarningTransitionRequested;

    public SettingsPageViewModel(
        BottomNavigationViewModel navigation,
        LocalizationService localization,
        IAppDataService dataService,
        IAppNavigationService navigationService,
        IApplicationLifecycleService lifecycleService,
        IDialogService dialogService,
        ILogger<SettingsPageViewModel> logger)
    {
        Navigation = navigation;
        Navigation.ActiveTab = NavigationTab.None;
        _localization = localization;
        _dataService = dataService;
        _navigationService = navigationService;
        _lifecycleService = lifecycleService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public BottomNavigationViewModel Navigation { get; }
    public string CurrentLanguageCode => _localization.LanguageCode;
    public string CurrentLanguageDisplayName =>
        _localization[_localization.LanguageCode == "en" ? "English" : "Russian"];
    public bool IsRussianSelected => CurrentLanguageCode == "ru";
    public bool IsEnglishSelected => CurrentLanguageCode == "en";
    public bool IsNotDeletingData => !IsDeletingData;

    public bool IsLanguagePanelVisible
    {
        get => _isLanguagePanelVisible;
        private set => SetProperty(ref _isLanguagePanelVisible, value);
    }

    public bool IsDeleteConfirmationVisible
    {
        get => _isDeleteConfirmationVisible;
        private set => SetProperty(ref _isDeleteConfirmationVisible, value);
    }

    public bool IsDeletingData
    {
        get => _isDeletingData;
        private set
        {
            if (SetProperty(ref _isDeletingData, value))
            {
                OnPropertyChanged(nameof(IsNotDeletingData));
            }
        }
    }

    [RelayCommand]
    private void ToggleLanguagePanel()
    {
        if (!IsDeletingData)
        {
            IsLanguagePanelVisible = !IsLanguagePanelVisible;
            IsDeleteConfirmationVisible = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ChangeLanguageAsync(string? languageCode)
    {
        if (IsDeletingData || languageCode is not ("ru" or "en"))
        {
            return;
        }

        if (_localization.LanguageCode == languageCode)
        {
            IsLanguagePanelVisible = false;
            return;
        }

        try
        {
            var languageSaved = await _localization.SaveLanguageAsync(languageCode);
            IsLanguagePanelVisible = false;
            await AwaitTransitionAsync(LanguageMenuTransitionRequested);

            if (languageSaved)
            {
                _localization.PrepareSavedLanguageForRestart(languageCode);
                await _lifecycleService.RestartApplicationAsync();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Changing the application language failed.");
            await _dialogService.ShowErrorAsync(
                _localization["ErrorTitle"],
                _localization["LanguageChangeFailed"],
                _localization["Ok"]);
        }
        finally
        {
            IsLanguagePanelVisible = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task BackAsync() => IsDeletingData
        ? Task.CompletedTask
        : _navigationService.GoBackAsync();

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task OpenPrivacyPolicyAsync() => IsDeletingData
        ? Task.CompletedTask
        : _navigationService.OpenPrivacyPolicyAsync();

    [RelayCommand]
    private void DeleteData()
    {
        if (!IsDeletingData)
        {
            IsLanguagePanelVisible = false;
            IsDeleteConfirmationVisible = true;
        }
    }

    [RelayCommand]
    private void CancelDelete() => IsDeleteConfirmationVisible = false;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ConfirmDeleteAsync()
    {
        if (IsDeletingData)
        {
            return;
        }

        IsDeletingData = true;
        try
        {
            await _dataService.DeleteAllUserDataAsync();
            IsDeleteConfirmationVisible = false;
            await AwaitTransitionAsync(DeleteWarningTransitionRequested);
            await MainThread.InvokeOnMainThreadAsync(_lifecycleService.CloseApplication);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Deleting all application user data failed.");
            await _dialogService.ShowErrorAsync(
                _localization["ErrorTitle"],
                _localization["DeleteFailed"],
                _localization["Ok"]);
        }
        finally
        {
            IsDeletingData = false;
        }
    }

    private static Task AwaitTransitionAsync(Func<Task>? transition) =>
        transition?.Invoke() ?? Task.CompletedTask;

    internal void ResetTransientUiState()
    {
        IsLanguagePanelVisible = false;
        IsDeleteConfirmationVisible = false;
    }
}
