using System.Text.RegularExpressions;
using CoffeeNap.Models;

namespace CoffeeNap.Helpers;

public static partial class UserNameValidator
{
    public const int MinimumLength = 3;

    public static string? GetValidationError(string? value)
    {
        var name = value?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return "Введите имя";
        }

        if (name.Length < MinimumLength)
        {
            return $"Имя должно содержать минимум {MinimumLength} символа";
        }

        if (name.Length > UserProfile.MaximumUserNameLength)
        {
            return $"Имя должно содержать не более {UserProfile.MaximumUserNameLength} символов";
        }

        return AllowedNameRegex().IsMatch(name)
            ? null
            : "Используйте только латинские буквы и цифры";
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedNameRegex();
}
