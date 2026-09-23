using CoffeeNap.Helpers;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoffeeNap.ViewModels;

// Объект употребления на главном экране. CaffeineConsumption прописан в Models
public sealed class ConsumptionItemViewModel(CaffeineConsumption model) : ObservableObject
{
    internal CaffeineConsumption Model { get; } = model;

    public int Id => Model.Id;

    public string Name => string.IsNullOrEmpty(Model.NameKey)
        ? Model.Name
        : LocalizationService.Current[Model.NameKey];

    public DateTimeOffset ConsumedAt => Model.ConsumedAt;

    public int CaffeineMg => Model.CaffeineMg;

    public CaffeineConsumptionType Type => Model.Type;

    public string Icon => Type switch
    {
        CaffeineConsumptionType.EnergyDrink => "energy_drink_ico.png",
        CaffeineConsumptionType.Tea => "tea_ico.png",
        _ => "coffee_ico.png"
    };

    public Color IconBackground => Type switch
    {
        CaffeineConsumptionType.EnergyDrink => Color.FromArgb("#5C5C5C"),
        CaffeineConsumptionType.Tea => Color.FromArgb("#777777"),
        _ => Colors.Black
    };

    public string CaffeineDisplay => $"{CaffeineMg} {LocalizationService.Current["MilligramShort"]}";

    private string? _relativeTime;
    public string RelativeTime => _relativeTime ??= RelativeTimeFormatter.Format(ConsumedAt, DateTimeOffset.Now);

    public void RefreshLocalizedState()
    {
        RefreshRelativeTime();
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(CaffeineDisplay));
    }

    public void RefreshRelativeTime() => SetProperty(ref _relativeTime,
        RelativeTimeFormatter.Format(ConsumedAt, DateTimeOffset.Now), nameof(RelativeTime));
}
