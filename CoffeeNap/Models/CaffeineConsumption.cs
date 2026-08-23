using CommunityToolkit.Mvvm.ComponentModel;
using CoffeeNap.Helpers;

namespace CoffeeNap.Models;

public sealed class CaffeineConsumption : ObservableObject
{
    private DateTimeOffset consumedAt;
    private int caffeineMg;
    private CaffeineConsumptionType type;

    public required string Name { get; init; }

    public required DateTimeOffset ConsumedAt
    {
        get => consumedAt;
        set
        {
            if (SetProperty(ref consumedAt, value))
            {
                OnPropertyChanged(nameof(RelativeTime));
            }
        }
    }

    public required int CaffeineMg
    {
        get => caffeineMg;
        set
        {
            if (SetProperty(ref caffeineMg, value))
            {
                OnPropertyChanged(nameof(CaffeineDisplay));
            }
        }
    }

    public required string Icon { get; init; }

    public required Color IconBackground { get; init; }

    public required CaffeineConsumptionType Type
    {
        get => type;
        set => SetProperty(ref type, value);
    }

    public string CaffeineDisplay => $"{CaffeineMg} мг";

    public string RelativeTime => RelativeTimeFormatter.Format(ConsumedAt, DateTimeOffset.Now);

    public void RefreshRelativeTime() => OnPropertyChanged(nameof(RelativeTime));
}
