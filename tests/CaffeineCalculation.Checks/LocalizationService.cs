// Only display strings are substituted; calculations, result builder and recipe mapper are production code.
namespace CoffeeNap.Services;
internal sealed class LocalizationService
{
    public static LocalizationService Current { get; } = new();
    public string this[string key] => key;
}
