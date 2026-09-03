using CoffeeNap.Services;

namespace CoffeeNap.Resources.Localization;

[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider) => new Binding
    {
        Path = $"[{Key}]",
        Source = LocalizationService.Current,
        Mode = BindingMode.OneWay
    };

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
