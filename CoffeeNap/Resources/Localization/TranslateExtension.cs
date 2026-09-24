using CoffeeNap.Services;

namespace CoffeeNap.Resources.Localization;

[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = string.Empty;

    // Creates a binding to the requested localized resource.
    public string ProvideValue(IServiceProvider serviceProvider) =>
        LocalizationService.Current[Key];

    // Creates a binding to the requested localized resource.
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
