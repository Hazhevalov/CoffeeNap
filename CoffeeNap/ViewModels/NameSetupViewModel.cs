using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace CoffeeNap.ViewModels;

/// <summary>Проверяет и сохраняет имя на втором шаге onboarding.</summary>
public partial class NameSetupViewModel : ObservableObject
{
    private string? name = string.Empty;
    private bool hasValidationError;
    private string validationMessage = string.Empty;

    public string? Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value) && HasValidationError)
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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ConfirmNameAsync()
    {
        var trimmedName = Name?.Trim() ?? string.Empty;
        var validationError = GetNameValidationError(trimmedName);
        if (validationError is not null)
        {
            ValidationMessage = validationError;
            HasValidationError = true;
            return;
        }

        Name = trimmedName;

        // Порядок важен: флаг завершения выставляется только после сохранения имени.
        UserPreferencesService.SetUserName(trimmedName);
        UserPreferencesService.SetOnboardingCompleted();

        // Абсолютный маршрут очищает onboarding из активного navigation stack.
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
