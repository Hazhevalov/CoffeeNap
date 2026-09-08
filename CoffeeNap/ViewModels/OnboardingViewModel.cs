using CoffeeNap.Helpers;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public enum OnboardingStep
{
    Welcome,
    NameSetup
}

/// <summary>Единая модель двух последовательных состояний onboarding.</summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly UserStateService _userState;
    private readonly IAppNavigationService _appNavigation;
    private readonly ILogger<OnboardingViewModel> _logger;
    private readonly LocalizationService _localization;
    private OnboardingStep _currentStep = OnboardingStep.Welcome;
    private string? _userName = string.Empty;
    private bool _hasValidationError;
    private string _validationMessage = string.Empty;

    public OnboardingViewModel(
        UserStateService userState,
        IAppNavigationService appNavigation,
        LocalizationService localization,
        ILogger<OnboardingViewModel> logger)
    {
        var startedAt = PerformanceTrace.Start();
        _userState = userState;
        _appNavigation = appNavigation;
        _localization = localization;
        _logger = logger;
        PerformanceTrace.Elapsed("OnboardingViewModel.ctor", startedAt);
    }

    public OnboardingStep CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    public string? UserName
    {
        get => _userName;
        set
        {
            if (SetProperty(ref _userName, value) && HasValidationError)
            {
                HasValidationError = false;
                ValidationMessage = string.Empty;
            }
        }
    }

    public bool HasValidationError
    {
        get => _hasValidationError;
        private set => SetProperty(ref _hasValidationError, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    [RelayCommand]
    private void Start()
    {
        if (CurrentStep == OnboardingStep.Welcome)
        {
            CurrentStep = OnboardingStep.NameSetup;
        }
    }

    // 
    [RelayCommand]
    private void ReturnToWelcome() => CurrentStep = OnboardingStep.Welcome;

    // Подтверждение имени, переход на основной экран
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ConfirmNameAsync()
    {
        var trimmedName = UserName?.Trim() ?? string.Empty;
        var validationError = UserNameValidator.GetValidationError(trimmedName);
        if (validationError is not null)
        {
            ValidationMessage = validationError;
            HasValidationError = true;
            return;
        }

        UserName = trimmedName;

        try
        {
            await _userState.CompleteOnboardingAsync(trimmedName);
        }
        catch (Exception exception)
        {
            ValidationMessage = _localization["ProfileSaveFailed"];
            HasValidationError = true;
            _logger.LogError(exception, "Profile save failed during onboarding.");
            return;
        }

        // Абсолютный маршрут не оставляет onboarding доступным через Back.
        await _appNavigation.NavigateToTopLevelAsync(AppShell.MainAbsoluteRoute);
    }
}
