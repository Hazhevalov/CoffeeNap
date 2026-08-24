using System.Text.RegularExpressions;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

public enum OnboardingStep
{
    Welcome,
    NameSetup
}

/// <summary>Единая модель двух последовательных состояний onboarding.</summary>
public partial class OnboardingViewModel : ObservableObject
{
    private OnboardingStep currentStep = OnboardingStep.Welcome;
    private string? userName = string.Empty;
    private bool hasValidationError;
    private string validationMessage = string.Empty;

    public OnboardingStep CurrentStep
    {
        get => currentStep;
        private set => SetProperty(ref currentStep, value);
    }

    public string? UserName
    {
        get => userName;
        set
        {
            if (SetProperty(ref userName, value) && HasValidationError)
            {
                HasValidationError = false;
                ValidationMessage = string.Empty;
            }
        }
    }

    public bool HasValidationError
    {
        get => hasValidationError;
        private set => SetProperty(ref hasValidationError, value);
    }

    public string ValidationMessage
    {
        get => validationMessage;
        private set => SetProperty(ref validationMessage, value);
    }

    [RelayCommand]
    private void Start()
    {
        if (CurrentStep == OnboardingStep.Welcome)
        {
            CurrentStep = OnboardingStep.NameSetup;
        }
    }

    [RelayCommand]
    private void ReturnToWelcome() => CurrentStep = OnboardingStep.Welcome;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ConfirmNameAsync()
    {
        var trimmedName = UserName?.Trim() ?? string.Empty;
        var validationError = GetNameValidationError(trimmedName);
        if (validationError is not null)
        {
            ValidationMessage = validationError;
            HasValidationError = true;
            return;
        }

        UserName = trimmedName;

        // Завершение записывается только после успешной проверки и сохранения имени.
        UserPreferencesService.SetUserName(trimmedName);
        UserPreferencesService.SetOnboardingCompleted();

        // Абсолютный маршрут не оставляет onboarding доступным через Back.
        await Shell.Current.GoToAsync("//MainPage/MainContent", true);
    }

    private static string? GetNameValidationError(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Введите имя";
        }

        if (name.Length < 3)
        {
            return "Имя должно содержать минимум 3 символа";
        }

        if (name.Length > 15)
        {
            return "Имя должно содержать не более 15 символов";
        }

        return AllowedNameRegex().IsMatch(name)
            ? null
            : "Используйте только латинские буквы и цифры";
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedNameRegex();
}
