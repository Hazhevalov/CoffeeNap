using System.Text.RegularExpressions;
using CoffeeNap.Models;
using CoffeeNap.Services;

namespace CoffeeNap.Helpers;

public static partial class UserNameValidator
{
    public const int MinimumLength = 3;

    public static string? GetValidationError(string? value)
    {
        var name = value?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return LocalizationService.Current["NameRequired"];
        }

        if (name.Length < MinimumLength)
        {
            return LocalizationService.Current.Format("NameTooShort", MinimumLength);
        }

        if (name.Length > UserProfile.MaximumUserNameLength)
        {
            return LocalizationService.Current.Format(
                "NameTooLong",
                UserProfile.MaximumUserNameLength);
        }

        return AllowedNameRegex().IsMatch(name)
            ? null
            : LocalizationService.Current["NameInvalidCharacters"];
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedNameRegex();
}
