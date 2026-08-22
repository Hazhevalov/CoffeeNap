using System.Collections.ObjectModel;
using CoffeeNap.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private double currentCaffeine = 120;
    private double dailyCaffeineLimit = 300;
    private CaffeineSourceStat coffeeSource = null!;
    private CaffeineSourceStat teaSource = null!;
    private CaffeineSourceStat energyDrinkSource = null!;

    public double CurrentCaffeine
    {
        get => currentCaffeine;
        set
        {
            if (SetProperty(ref currentCaffeine, value))
            {
                OnPropertyChanged(nameof(DailyProgress));
            }
        }
    }

    public double DailyCaffeineLimit
    {
        get => dailyCaffeineLimit;
        set
        {
            if (SetProperty(ref dailyCaffeineLimit, value))
            {
                OnPropertyChanged(nameof(DailyProgress));
            }
        }
    }

    public CaffeineSourceStat CoffeeSource
    {
        get => coffeeSource;
        private set => SetProperty(ref coffeeSource, value);
    }

    public CaffeineSourceStat TeaSource
    {
        get => teaSource;
        private set => SetProperty(ref teaSource, value);
    }

    public CaffeineSourceStat EnergyDrinkSource
    {
        get => energyDrinkSource;
        private set => SetProperty(ref energyDrinkSource, value);
    }

    public MainViewModel()
    {
        SourceStats = new ObservableCollection<CaffeineSourceStat>();
        Consumptions = new ObservableCollection<CaffeineConsumption>();

        LoadSourceStats();
        LoadConsumptions();
    }

    public double DailyProgress => DailyCaffeineLimit <= 0
        ? 0
        : Math.Clamp(CurrentCaffeine / DailyCaffeineLimit, 0, 1);

    public ObservableCollection<CaffeineSourceStat> SourceStats { get; }

    public ObservableCollection<CaffeineConsumption> Consumptions { get; }

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(nameof(SettingsPage));

    [RelayCommand]
    private Task OpenAddConsumptionAsync() => Shell.Current.GoToAsync(nameof(AddConsumptionPage));

    [RelayCommand]
    private Task OpenCalendarAsync() => Shell.Current.GoToAsync(nameof(CalendarPage));

    private void LoadSourceStats()
    {
        var values = new[]
        {
            (Name: "Кофе", Amount: 120d, Color: Color.FromArgb("#111111")),
            (Name: "Чай", Amount: 120d, Color: Color.FromArgb("#686868")),
            (Name: "Энергетики", Amount: 120d, Color: Color.FromArgb("#B8B8B8"))
        };

        var total = values.Sum(item => item.Amount);

        CoffeeSource = new CaffeineSourceStat(values[0].Name, values[0].Amount, total, values[0].Color);
        TeaSource = new CaffeineSourceStat(values[1].Name, values[1].Amount, total, values[1].Color);
        EnergyDrinkSource = new CaffeineSourceStat(values[2].Name, values[2].Amount, total, values[2].Color);

        SourceStats.Clear();
        SourceStats.Add(CoffeeSource);
        SourceStats.Add(TeaSource);
        SourceStats.Add(EnergyDrinkSource);
    }

    private void LoadConsumptions()
    {
        var now = DateTimeOffset.Now;

        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Латте на кокосовом",
            CaffeineMg = 120,
            ConsumedAt = now.AddMinutes(-15),
            Icon = "coffee_ico.png",
            IconBackground = Colors.Black
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Энергетик (500мл)",
            CaffeineMg = 160,
            ConsumedAt = now.AddDays(-1),
            Icon = "energy_drink_ico.png",
            IconBackground = Color.FromArgb("#5C5C5C")
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Эспрессо",
            CaffeineMg = 65,
            ConsumedAt = now.AddDays(-1).AddHours(-3),
            Icon = "coffee_ico.png",
            IconBackground = Colors.Black
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Зелёный чай",
            CaffeineMg = 35,
            ConsumedAt = now.AddDays(-2),
            Icon = "tea_ico.png",
            IconBackground = Color.FromArgb("#777777")
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Капучино",
            CaffeineMg = 80,
            ConsumedAt = now.AddDays(-2).AddHours(-4),
            Icon = "coffee_ico.png",
            IconBackground = Colors.Black
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Чёрный чай",
            CaffeineMg = 45,
            ConsumedAt = now.AddDays(-3),
            Icon = "tea_ico.png",
            IconBackground = Color.FromArgb("#777777")
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Американо",
            CaffeineMg = 95,
            ConsumedAt = now.AddDays(-4),
            Icon = "coffee_ico.png",
            IconBackground = Colors.Black
        });
        Consumptions.Add(new CaffeineConsumption
        {
            Name = "Энергетик (250мл)",
            CaffeineMg = 80,
            ConsumedAt = now.AddDays(-5),
            Icon = "energy_drink_ico.png",
            IconBackground = Color.FromArgb("#5C5C5C")
        });
    }
}
